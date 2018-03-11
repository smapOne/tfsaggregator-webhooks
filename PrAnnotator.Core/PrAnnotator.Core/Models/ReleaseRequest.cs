using System;
using Newtonsoft.Json.Linq;

namespace PrAnnotator.Core.Models
{
    public class ReleaseRequest : IPrAnnotatorModel
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


        public string ReleaseUri { get; set; }

        public int ReleaseId { get; set; }

        public string TeamProject { get; set; }

        public string Environment { get; set; }

        public string ReleaseName { get; set; }

        private ReleaseRequest()
        {
            this.Error = string.Empty;
        }

        public static ReleaseRequest Parse(JObject payload)
        {
            var result = new ReleaseRequest();

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

                string fullUrl = (string)payload["resource"]["url"];
                result.TfsCollectionUri = fullUrl.Substring(0, fullUrl.IndexOf("_apis", StringComparison.Ordinal));

                switch (result.EventType)
                {
                    case "ms.vss-release.deployment-started-event":
                    case "ms.vss-release.deployment-completed-event":
                        result.Environment = (string) payload["resource"]["environment"]["name"];
                        result.Status = (string) payload["resource"]["environment"]["status"];
                        result.ReleaseId = (int)payload["resource"]["release"]["id"];
                        result.ReleaseUri = (string) payload["resource"]["release"]["url"];
                        result.ReleaseName = (string)payload["resource"]["release"]["name"];
                        result.TeamProject = (string) payload["project"]["name"];
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