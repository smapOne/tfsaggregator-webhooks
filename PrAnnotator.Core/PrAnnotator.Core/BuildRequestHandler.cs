using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using PrAnnotator.Core.Models;

namespace PrAnnotator.Core
{
    public class BuildRequestHandler
    {
        public async Task HandleBuild(BuildRequest build, string pat, Uri projectCollectionUri)
        {
            if (string.IsNullOrEmpty(build.SourceBranch))
            {
                return;
            }

            var creds = new VssBasicCredential(string.Empty, pat);
            var connection = new VssConnection(new Uri(projectCollectionUri, "DefaultCollection"), creds);

            var gitClient = connection.GetClient<GitHttpClient>();

            var prs = await gitClient.GetPullRequestsAsync(build.TeamProject, build.RepositoryId,
                new GitPullRequestSearchCriteria {SourceRefName = build.SourceBranch, Status = PullRequestStatus.Active});

            foreach (var pr in prs)
            {
                // Delete Status from PRCreate
                var oldstatuses = await gitClient.GetPullRequestStatusesAsync(build.TeamProject, build.RepositoryId, pr.PullRequestId);
                var toDeleteStatuses = oldstatuses.Where(s => s.Context.Name == "PrAnnotator");
                foreach (var oldstatus in toDeleteStatuses)
                {
                    await gitClient.DeletePullRequestStatusAsync(build.TeamProject, build.RepositoryId, pr.PullRequestId, oldstatus.Id);
                }

                var prStatus = new GitPullRequestStatus
                {
                    State = build.Status == "completed" ? GitStatusState.Succeeded : GitStatusState.Failed,
                    Description = $"{build.DefinitionName}: Build {build.Status}",
                    Context = new GitStatusContext {Genre = "Build", Name = build.DefinitionName},
                    TargetUrl = build.BuildUri
                };

                //await gitClient.CreatePullRequestStatusAsync(prStatus, build.TeamProject, pr.Repository.Id, pr.PullRequestId).ConfigureAwait(false);

                var iterations = await gitClient.GetPullRequestIterationsAsync(pr.Repository.Id, pr.PullRequestId);
                var id = iterations.Last(i => i.SourceRefCommit.CommitId == build.SourceVersion).Id;
                if (id != null)
                {
                    prStatus.Description = $"Update {id.Value}: {prStatus.Description}";
                    await gitClient.CreatePullRequestIterationStatusAsync(prStatus, pr.Repository.Id, pr.PullRequestId, id.Value);
                }
            }
        }
    }
}
