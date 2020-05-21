using System.Collections.Generic;
using System.Net.Mime;
using EventSourcing.Common;
using EventSourcing.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EventSourcing.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConferenceController : ControllerBase
    {
        private readonly IConferenceService _conferenceService;
        private readonly IConferenceCosmosDbService _conferenceCosmosDbService;

        public ConferenceController(IConferenceService conferenceService, IConferenceCosmosDbService conferenceCosmosDbService)
        {
            _conferenceService = conferenceService;
            _conferenceCosmosDbService = conferenceCosmosDbService;
        }

        [HttpGet, Produces(typeof(List<ConferenceDataModel>))]
        public async Task<List<ConferenceDataModel>> Get()
        {
            //return Ok(_conferenceService.GetAllConferences());

            var service = new ProjectionService();

            return await service.GetAllConferences();
        }

        [HttpPut]
        public async Task<IActionResult> Put(ConferenceModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Data.Id)) return BadRequest();

            var streamId = _conferenceService.GetConferenceId(model);

            if (model.Data.Seats > 0)
            {
                var availableSeats = _conferenceService.GetAvailableSeats(streamId);
                if (model.Event.Equals("Conference.SeatsRemoved") && model.Data.Seats > availableSeats)
                    return BadRequest("Not enough seats available");
            }

            var sequence = _conferenceService.GetNext(streamId);

            var insertedEntity = await _conferenceService.InsertEntityAsync(streamId, sequence, model);

            await _conferenceService.InsertQueueMessageAsync(streamId, sequence);

            return Ok(insertedEntity);
        }

        [HttpPatch]
        public async Task<IActionResult> Patch(string streamId)
        {
            var service = new ProjectionService();

            await service.CreateConferenceProjection(streamId);
            //if (model == null || string.IsNullOrEmpty(model.Data.Id)) return BadRequest();

            //var streamId = _conferenceService.GetConferenceId(model);

            //if (model.Data.Seats > 0)
            //{
            //    var availableSeats = _conferenceService.GetAvailableSeats(streamId);
            //    if (model.Event.Equals("Conference.SeatsRemoved") && model.Data.Seats > availableSeats)
            //        return BadRequest("Not enough seats available");
            //}

            //var sequence = _conferenceService.GetNext(streamId);

            //var insertedEntity = await _conferenceService.InsertEntityAsync(streamId, sequence, model);

            //await _conferenceService.InsertQueueMessageAsync(streamId, sequence);

            //return Ok(insertedEntity);

            return Ok();
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[Produces(typeof(CosmosEntities.ConferenceEntity))]
        public async Task<IActionResult> Post(ConferenceModel model)
        {
            if (model == null) return BadRequest();

            var insertedEntity = await _conferenceCosmosDbService.InsertAsync(model);

            //return Ok(insertedEntity);

            return CreatedAtAction(nameof(Get), new { id = insertedEntity.Resource.Id }, insertedEntity.Resource);

            //var streamId = _conferenceService.GetConferenceId(model);

            //var sequence = _conferenceService.GetNext(streamId);

            //var insertedEntity = await _conferenceService.InsertEntityAsync(streamId, sequence, model);

            //await _conferenceService.InsertQueueMessageAsync(streamId, sequence);

            //return Ok(insertedEntity);
        }

    }
}
