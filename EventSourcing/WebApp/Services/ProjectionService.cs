using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Common;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace EventSourcing.Services
{
    public class ProjectionService
    {
        private const string CosmosDatabaseId = "EventSourcing";
        private const string ContainerId = "data";
        private const string Endpoint = "https://localhost:8081/";
        private const string AuthKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        private readonly Container _container;

        public ProjectionService()
        {
            var client = new CosmosClient(Endpoint, AuthKey);
            _container = client.GetContainer(CosmosDatabaseId, ContainerId);
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
