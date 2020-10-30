 using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Common;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace EventSourcing.CosmosDb.Services
{
    public interface ICosmosDbProjectionService
    {
        Task CreateConferenceProjection(string streamId);
        Task<List<ConferenceDataModel>> GetAllConferences();
        Task<ConferenceDataModel> GetConference(string streamId);
    }

    public class CosmosDbProjectionService : ICosmosDbProjectionService
    {
        private readonly Container _container;

        public CosmosDbProjectionService(IOptions<Settings.CosmosSettings> options)
        {
            var cosmosSettings = options.Value;

            var client = new CosmosClient(cosmosSettings.Endpoint, cosmosSettings.AuthKey);
            _container = client.GetContainer(cosmosSettings.CosmosDatabaseId, cosmosSettings.ContainerId);
        }

        public async Task<List<ConferenceDataModel>> GetAllConferences()
        {
            const string sqlQueryText = "SELECT * FROM c WHERE c.partitionKey = 'Projections.Conference'";

            var queryDefinition = new QueryDefinition(sqlQueryText);

            var queryResultSetIterator = _container.GetItemQueryIterator<CosmosEntities.ConferenceProjectionEntity>(queryDefinition);

            var conferences = new List<ConferenceDataModel>();

            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet = await queryResultSetIterator.ReadNextAsync();

                conferences
                    .AddRange(currentResultSet.Select(document => new ConferenceDataModel
                    {
                        Id = document.ConferenceDataModel.Id,
                        Name = document.ConferenceDataModel.Name,
                        Seats = document.ConferenceDataModel.Seats
                    }));
            }

            return conferences;
        }

        public async Task<ConferenceDataModel> GetConference(string streamId)
        {
            var sqlQueryText = $"SELECT * FROM c WHERE c.partitionKey = 'Projections.Conference' AND c.id = 'conference-{streamId}'";

            var queryDefinition = new QueryDefinition(sqlQueryText);

            var queryResultSetIterator = _container.GetItemQueryIterator<CosmosEntities.ConferenceProjectionEntity>(queryDefinition);

            var conference = new ConferenceDataModel();

            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet = await queryResultSetIterator.ReadNextAsync();

                conference =  currentResultSet.Select(document => new ConferenceDataModel
                {
                    Id = document.ConferenceDataModel.Id,
                    Name = document.ConferenceDataModel.Name,
                    Seats = document.ConferenceDataModel.Seats
                }).SingleOrDefault();
            }

            return conference;
        }

        public async Task CreateConferenceProjection(string streamId)
        {
            var sqlQueryText = $"SELECT * FROM Events c WHERE c.partitionKey = '{streamId}' ORDER BY c.sequenceNumber";

            var queryDefinition = new QueryDefinition(sqlQueryText);

            var queryResultSetIterator = _container.GetItemQueryIterator<CosmosEntities.ConferenceEntity>(queryDefinition);

            var dataModel = new ConferenceDataModel();

            var lastSequenceRun = 0;

            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (var document in currentResultSet)
                {
                    lastSequenceRun = document.SequenceNumber;
                    ConferenceModel data;

                    switch (document.ConferenceModel.Event)
                    {
                        case "Conference.Created":
                            dataModel = document.ConferenceModel.Data;
                            break;
                        case "Conference.SeatsAdded":
                            data = document.ConferenceModel;
                            dataModel.Seats += data.Data.Seats;
                            break;
                        case "Conference.SeatsRemoved":
                            data = document.ConferenceModel;
                            dataModel.Seats -= data.Data.Seats;
                            break;
                    }
                }
            }

            var entity = new CosmosEntities.ConferenceProjectionEntity()
            {
                Id = streamId,
                PartitionKey = "Projections.Conference",
                ConferenceDataModel = dataModel
            };

            await _container.UpsertItemAsync(entity);
        }
    }
}
