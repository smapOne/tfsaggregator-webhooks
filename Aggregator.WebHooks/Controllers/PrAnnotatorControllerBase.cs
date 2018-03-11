using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Aggregator.Core.Context;
using Aggregator.Core.Monitoring;
using Aggregator.Models;
using Aggregator.WebHooks.Utils;
using PrAnnotator.Core.Models;

namespace Aggregator.WebHooks.Controllers
{
    public class PrAnnotatorControllerBase : ApiController
    {
        public HttpResponseMessage Get()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent($"Hello from TFSAggregator2webHooks @{Environment.MachineName}");
            return response;
        }

        protected RuntimeContext GetRuntimeContext(IPrAnnotatorModel request)
        {
            string policyFilePath = System.Configuration.ConfigurationManager.AppSettings["policyFilePath"];
            // macro expansion to permit multi-tenants
            string policyFile = policyFilePath.WithVar(request);

            // cache requires absolute path
            policyFile = System.Web.Hosting.HostingEnvironment.MapPath(policyFile);
            Debug.Assert(System.IO.File.Exists(policyFile));

            // need a logger to show errors in config file (Catch 22)
            var logger = new AspNetEventLogger(request.EventId, LogLevel.Normal);

            var context = new RequestContext(request.TfsCollectionUri, request.TeamProject);
            var runtime = RuntimeContext.GetContext(
                () => policyFile,
                context,
                logger,
                (runtimeContext) => null,
                (runtimeContext) => null);
            if (runtime.HasErrors)
            {
                this.Log(runtime.Errors.Current);
                return runtime;
            }
            return runtime;
        }

        protected void Log(string message)
        {
            Trace.WriteLine(message);
            EventLog.WriteEntry("TFSAggregator", message, EventLogEntryType.Warning, 42);
        }
    }
}