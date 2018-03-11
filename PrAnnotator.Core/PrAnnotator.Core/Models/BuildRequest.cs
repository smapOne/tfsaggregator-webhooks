using System;
using Newtonsoft.Json.Linq;

namespace PrAnnotator.Core.Models
{
    public class BuildRequest : IPrAnnotatorModel
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
        public string TfsCollectionUri { get; private set; }

        public string SourceRefName { get; set; }

        public string Status { get; set; }


        public string BuildUri { get; set; }

        public int BuildId { get; set; }

        public string SourceVersion { get; set; }

        public string DefinitionName { get; set; }

        public string SourceBranch { get; set; }

        public string TeamProject { get; set; }

        public string RepositoryId { get; set; }

        private BuildRequest()
        {
            this.Error = string.Empty;
        }

        public static BuildRequest Parse(JObject payload)
        {
            var result = new BuildRequest();

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

                result.TfsCollectionUri = (string)payload["resourceContainers"]["project"]["baseUrl"];

                switch (result.EventType)
                {
                    case "build.complete":
                        result.BuildUri = (string) payload["resource"]["url"];
                        result.BuildId = (int) payload["resource"]["id"];
                        result.Status = (string) payload["resource"]["status"];
                        result.SourceVersion = (string) payload["resource"]["sourceVersion"];
                        result.SourceBranch = (string) payload["resource"]["sourceBranch"];
                        result.DefinitionName = (string) payload["resource"]["definition"]["name"];
                        result.TeamProject = (string) payload["resource"]["project"]["name"];
                        result.RepositoryId = (string) payload["resource"]["repository"]["id"];
                break;
                    default:
                        result.Error = $"Unsupported eventType {result.EventType}";
                        break;
                }//switch

            }//if
            return result;
        }
    }
}