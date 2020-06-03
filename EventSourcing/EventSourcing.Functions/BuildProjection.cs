using EventSourcing.Common;
using EventSourcing.Table.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace EventSourcing.Functions
{
    public static class BuildProjection
    {
        [FunctionName("BuildProjection")]
        public static async Task<IActionResult> Run([QueueTrigger("eventsourcing-queue", Connection = "LocalStorage")] string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

            var message = JsonConvert.DeserializeObject<QueueEntities.Message>(myQueueItem);

            await new TableProjectionService().BuildProjection(message);

            return new OkObjectResult("");
        }
    }
}
