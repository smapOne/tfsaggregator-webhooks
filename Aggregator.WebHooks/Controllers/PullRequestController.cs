using BasicAuthentication.Filters;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using PrAnnotator.Core;
using PrAnnotator.Core.Models;

namespace Aggregator.WebHooks.Controllers
{
    public class PullRequestController : PrAnnotatorControllerBase
    {
	    [IdentityBasicAuthentication] // Enable authentication via an ASP.NET Identity user name and password
	    [Authorize] // Require some form of authentication
        public async Task<HttpResponseMessage> Post([FromBody]JObject payload)
        {
            var request = PullRequest.Parse(payload);

            if (!request.IsValid)
            {
                this.Log(request.Error);
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { ReasonPhrase = request.Error };
            }

            var runtime = this.GetRuntimeContext(request);

            await new PullRequestCreatedHandler().HandleNewPr(request, runtime.Settings.PersonalToken, runtime.RequestContext.GetProjectCollectionUri());

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
