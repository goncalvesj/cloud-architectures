using EventSourcing.Table.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace EventSourcing.Functions
{
    public static class RebuildProjections
    {
        [FunctionName("RebuildProjections")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            await new TableProjectionService().RebuildProjections();

            return new OkObjectResult("");
        }
    }
}
