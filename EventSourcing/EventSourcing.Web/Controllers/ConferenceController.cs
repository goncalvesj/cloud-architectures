using EventSourcing.Common;
using EventSourcing.CosmosDb.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;

namespace EventSourcing.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConferenceController : ControllerBase
    {
        private readonly IConferenceCosmosDbService _conferenceCosmosDbService;
        private readonly ICosmosDbProjectionService _cosmosDbProjectionService;

        public ConferenceController(IConferenceCosmosDbService conferenceCosmosDbService, ICosmosDbProjectionService cosmosDbProjectionService)
        {
            _conferenceCosmosDbService = conferenceCosmosDbService;
            _cosmosDbProjectionService = cosmosDbProjectionService;
        }

        [HttpGet, Produces(typeof(List<ConferenceDataModel>))]
        public async Task<List<ConferenceDataModel>> Get()
        {
            return await _cosmosDbProjectionService.GetAllConferences();
        }

        [HttpPut]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Put(ConferenceModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Data.Id)) return BadRequest();

            // Validate Seats
            var conf = await _cosmosDbProjectionService.GetConference(model.Data.Id);

            if (model.Event.Equals("Conference.SeatsRemoved") && model.Data.Seats > conf.Seats)
                return BadRequest("Not enough seats available");

            var insertedEntity = await _conferenceCosmosDbService.InsertAsync(model);

            await _cosmosDbProjectionService.CreateConferenceProjection(insertedEntity.Resource.PartitionKey);

            return CreatedAtAction(nameof(Put), new { id = insertedEntity.Resource.Id }, insertedEntity.Resource);
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post(ConferenceModel model)
        {
            if (model == null) return BadRequest();

            var insertedEntity = await _conferenceCosmosDbService.InsertAsync(model);

            await _cosmosDbProjectionService.CreateConferenceProjection(insertedEntity.Resource.PartitionKey);

            return CreatedAtAction(nameof(Post), new { id = insertedEntity.Resource.Id }, insertedEntity.Resource);
        }

    }
}
