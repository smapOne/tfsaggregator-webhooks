﻿namespace Aggregator.WebHooks.Controllers
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Newtonsoft.Json.Linq;
    using PrAnnotator.Core;
    using PrAnnotator.Core.Models;

    public class ReleaseController : PrAnnotatorControllerBase
    {
        [Authorize] // Require some form of authentication
        public async Task<HttpResponseMessage> Post([FromBody]JObject payload)
        {
            var request = ReleaseRequest.Parse(payload);

            if (!request.IsValid)
            {
                this.Log(request.Error);
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { ReasonPhrase = request.Error };
            }

            var runtime = this.GetRuntimeContext(request);

            await new ReleaseRequestHandler().HandleRelease(request, runtime.Settings.PersonalToken, runtime.RequestContext.GetProjectCollectionUri());

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
