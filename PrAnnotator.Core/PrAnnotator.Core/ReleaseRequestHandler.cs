using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.WebApi;
using PrAnnotator.Core.Models;

namespace PrAnnotator.Core
{
    public class ReleaseRequestHandler
    {
        public async Task HandleBuild(ReleaseRequest releaseRequest, string pat, Uri projectCollectionUri)
        {
            var creds = new VssBasicCredential(string.Empty, pat);
            var connection = new VssConnection(new Uri(projectCollectionUri, "DefaultCollection"), creds);

            var gitClient = connection.GetClient<GitHttpClient>();
            var releaseClient = connection.GetClient<ReleaseHttpClient>();


            var release = await releaseClient.GetReleaseAsync(releaseRequest.TeamProject, releaseRequest.ReleaseId);
            var build = release.Artifacts.First(a => a.Type == "Build").DefinitionReference;
            var branch = build["branch"].Name;

            var envStatus = release.Environments.ToDictionary(e => e.Name, e => e.Status);

            var repos = await gitClient.GetRepositoriesAsync(releaseRequest.TeamProject);

            var prs = await gitClient.GetPullRequestsAsync(
                releaseRequest.TeamProject,
                repos.First(r => r.Name == releaseRequest.TeamProject).Id,
                new GitPullRequestSearchCriteria {Status = PullRequestStatus.Active, SourceRefName = branch});

            foreach (var pr in prs)
            {
                foreach (var env in envStatus)
                {
                    var prStatus = new GitPullRequestStatus
                    {
                        State = GetEnvState(env.Value),
                        Description = $"Deploy {env.Key} {env.Value}",
                        TargetUrl = (release.Links.Links["web"] as ReferenceLink)?.Href,
                        Context = new GitStatusContext { Genre = "release", Name = env.Key }
                    };

                    await gitClient.CreatePullRequestStatusAsync(prStatus, releaseRequest.TeamProject, pr.Repository.Id, pr.PullRequestId).ConfigureAwait(false);

                    var iterations = await gitClient.GetPullRequestIterationsAsync(pr.Repository.Id, pr.PullRequestId);
                    var id = iterations.Last()?.Id;
                    if (id != null)
                    {
                        prStatus.Description = prStatus.Description + " Iteration: " + id.Value;
                        await gitClient.CreatePullRequestIterationStatusAsync(prStatus, pr.Repository.Id, pr.PullRequestId, id.Value);
                    }
                }
            }
        }

        private static GitStatusState GetEnvState(EnvironmentStatus env)
        {
            switch (env)
            {
                case EnvironmentStatus.Undefined:
                case EnvironmentStatus.NotStarted:
                    return GitStatusState.NotSet;
                case EnvironmentStatus.Scheduled:
                case EnvironmentStatus.Queued:
                case EnvironmentStatus.InProgress:
                    return GitStatusState.Pending;
                case EnvironmentStatus.PartiallySucceeded:
                case EnvironmentStatus.Succeeded:
                    return GitStatusState.Succeeded;
                case EnvironmentStatus.Canceled:
                case EnvironmentStatus.Rejected:
                    return GitStatusState.Failed;
                default:
                    return GitStatusState.Error;
            }
        }
    }
}
