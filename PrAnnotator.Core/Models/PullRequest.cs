using System;
using Newtonsoft.Json.Linq;

namespace PrAnnotator.Core.Models
{
    public class PullRequest : IPrAnnotatorModel
    {
        // pseudo-data
        public bool IsValid => string.IsNullOrWhiteSpace(this.Error);

        public string Error { get; private set; }
        // real data
        public string EventId { get; private set; }
        public string EventType { get; private set; }
        // EventType property decoded
        public string AccountId { get; private set; }
        public string CollectionId { get; private set; }
        public int PullrequestId { get; private set; }
        public string TeamProject { get; private set; }
        public string TfsCollectionUri { get; private set; }

        public string MergeStatus { get; set; }

        public string TargetRefName { get; set; }

        public string SourceRefName { get; set; }

        public string Status { get; set; }
        public Event EventCategory { get; set; }


        private PullRequest()
        {
            this.Error = string.Empty;
        }

        public static PullRequest Parse(JObject payload)
        {
            var result = new PullRequest();

            if (payload.Property("eventType") == null)
            {
                result.Error = $"Could not determine event type for message: {payload}";
            }
            else
            {
                result.EventType = (string)payload["eventType"];
                result.EventId = (string)payload["id"];

                // TODO in the future we will use also the Organization level
                if (payload.Property("resourceContainers") == null)
                {
                    // bloody Test button
                    result.Error = $"Test button generates bad messages: do not use with this service.";
                }
                else
                {
                    // VSTS sprint 100 or so introduced the Account, but TFS 2015.3 stil lacks it
                    if (payload.SelectToken("resourceContainers.account") == null)
                    {
                        result.CollectionId = (string)payload["resourceContainers"]["collection"]["id"];
                    }
                    else
                    {
                        result.AccountId = (string)payload["resourceContainers"]["account"]["id"];
                    }
                }

                //string fullUrl = (string)payload["resource"]["url"];
                //fullUrl.Substring(0, fullUrl.IndexOf("_apis", StringComparison.Ordinal));
                result.TfsCollectionUri = result.TfsCollectionUri = (string)payload["resourceContainers"]["project"]["baseUrl"];

                switch (result.EventType)
                {
                    case "git.pullrequest.created":
                        result.EventCategory = Event.Created;
                        result.PullrequestId = (int)payload["resource"]["pullRequestId"];
                        result.TeamProject = (string)payload["resource"]["repository"]["project"]["name"];
                        result.Status = (string) payload["resource"]["status"];
                        result.SourceRefName = (string) payload["resource"]["sourceRefName"];
                        result.TargetRefName = (string) payload["resource"]["targetRefName"];
                        result.MergeStatus = (string) payload["resource"]["mergeStatus"];
                        break;
                    case "git.pullrequest.updated":
                        result.EventCategory = Event.Updated;
                        result.PullrequestId = (int)payload["resource"]["pullRequestId"];
                        result.TeamProject = (string)payload["resource"]["repository"]["project"]["name"];
                        result.Status = (string)payload["resource"]["status"];
                        result.SourceRefName = (string)payload["resource"]["sourceRefName"];
                        result.TargetRefName = (string)payload["resource"]["targetRefName"];
                        result.MergeStatus = (string)payload["resource"]["mergeStatus"];
                        break;
                    default:
                        result.Error = $"Unsupported eventType {result.EventType}";
                        break;
                }//switch

            }//if
            return result;
        }
    }

    public enum Event
    {
        Created,
        Updated
    }
}