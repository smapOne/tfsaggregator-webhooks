using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
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
        public async Task HandleRelease(ReleaseRequest releaseRequest, string pat, Uri projectCollectionUri)
        {
            var creds = new VssBasicCredential(string.Empty, pat);
            var connection = new VssConnection(new Uri(projectCollectionUri, "DefaultCollection"), creds);

            var gitClient = connection.GetClient<GitHttpClient>();
            var buildClient = connection.GetClient<BuildHttpClient>();
            var releaseClient = connection.GetClient<ReleaseHttpClient>();


            var release = await releaseClient.GetReleaseAsync(releaseRequest.TeamProject, releaseRequest.ReleaseId);
            var buildArtifact = release.Artifacts.First(a => a.Type == "Build").DefinitionReference;
            var branch = buildArtifact["branch"].Name;

            var build = await buildClient.GetBuildAsync(buildArtifact["project"].Id, int.Parse(buildArtifact["version"].Id));

            var envStatus = release.Environments.ToDictionary(e => e.Name, e => e.Status);

            var repos = await gitClient.GetRepositoriesAsync(releaseRequest.TeamProject);

            var prs = await gitClient.GetPullRequestsAsync(
                releaseRequest.TeamProject,
                repos.First(r => r.Name == releaseRequest.TeamProject).Id,
                new GitPullRequestSearchCriteria {Status = PullRequestStatus.Active, SourceRefName = branch});

            foreach (var pr in prs)
            {
                var iterations = await gitClient.GetPullRequestIterationsAsync(pr.Repository.Id, pr.PullRequestId);
                var id = iterations.Last(i => i.SourceRefCommit.CommitId == build.SourceVersion).Id;

                foreach (var env in envStatus)
                {
                    var prStatus = new GitPullRequestStatus
                    {
                        State = GetEnvState(env.Value),
                        Description = $"Deploy {env.Key} {env.Value}",
                        TargetUrl = (release.Links.Links["web"] as ReferenceLink)?.Href,
                        Context = new GitStatusContext { Genre = "release", Name = env.Key }
                    };

                    if (id != null)
                    {
                        prStatus.Description = $"{id.Value}: {prStatus.Description}";
                        await gitClient.CreatePullRequestIterationStatusAsync(prStatus, pr.Repository.Id, pr.PullRequestId, id.Value);
                    }
                }

                if (envStatus.All(e => GetEnvState(e.Value) == GitStatusState.Succeeded))
                {
                    var prStatus = new GitPullRequestStatus
                    {
                        State = GitStatusState.Succeeded,
                        Description = $"Last Complete Deploy: Update {id}",
                        TargetUrl = (release.Links.Links["web"] as ReferenceLink)?.Href,
                        Context = new GitStatusContext { Genre = "release", Name = "lastComplete" }
                    };

                    await gitClient.CreatePullRequestStatusAsync(prStatus, releaseRequest.TeamProject, pr.Repository.Id, pr.PullRequestId).ConfigureAwait(false);
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
