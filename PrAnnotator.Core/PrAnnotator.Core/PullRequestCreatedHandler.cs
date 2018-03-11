using System;
using System.Linq;
using System.Threading.Tasks;
using Aggregator.Core.Configuration;
using Aggregator.Core.Facade;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using PrAnnotator.Core.Models;

namespace PrAnnotator.Core
{
    public class PullRequestCreatedHandler
    {
        public async Task HandleNewPr(PullRequest prRequest, string pat, Uri projectCollectionUri)
        {
            var creds = new VssBasicCredential(string.Empty, pat);
            var connection = new VssConnection(new Uri(projectCollectionUri, "DefaultCollection"), creds);

            var gitClient = connection.GetClient<GitHttpClient>();

            var pr = await gitClient.GetPullRequestByIdAsync(prRequest.PullrequestId);

            var iterations = await gitClient.GetPullRequestIterationsAsync(pr.Repository.Id, prRequest.PullrequestId, true);
            var prStatus = new GitPullRequestStatus
            {
                State = GitStatusState.NotSet,
                Description = "No finished Build",
                Context = new GitStatusContext {Genre = "continous-integration", Name = "PrAnnotator"}
            };

            var status = await gitClient.CreatePullRequestStatusAsync(prStatus, prRequest.TeamProject, pr.Repository.Id, pr.PullRequestId).ConfigureAwait(false);
            var id = iterations.Last()?.Id;
            if (id != null)
            {
                prStatus.Description = prStatus.Description + "Iteration: " + id.Value;
                await gitClient.CreatePullRequestIterationStatusAsync(prStatus, pr.Repository.Id, pr.PullRequestId, id.Value);
            }
        }
    }
}
