using System;
using System.Threading.Tasks;
using EventSourcing.Common;
using Microsoft.Azure.Cosmos;

namespace EventSourcing.CosmosDb.Services
{
    public interface IConferenceCosmosDbService
    {
        Task<ItemResponse<CosmosEntities.ConferenceEntity>> InsertAsync(ConferenceModel model);
    }

    public class CosmosDbConferenceService : IConferenceCosmosDbService
    {
        private const string CosmosDatabaseId = "EventSourcing";
        private const string ContainerId = "data";
        private const string Endpoint = "https://localhost:8081/";
        private const string AuthKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        private readonly Container _container;

        public CosmosDbConferenceService()
        {
            var client = new CosmosClient(Endpoint, AuthKey);
            _container = client.GetContainer(CosmosDatabaseId, ContainerId);
        }

        private string GetConferenceId(ConferenceModel model)
        {
            var id = !string.IsNullOrEmpty(model.Data.Id) ? model.Data.Id : Guid.NewGuid().ToString();
            model.Data.Id = id;
            return $"conference-{id}";
        }

        private async Task<int> GetNextAsync(string eventStream)
        {
            var sqlQueryText = $"SELECT TOP 1 * FROM c WHERE c.partitionKey = '{eventStream}' ORDER BY c.sequenceNumber DESC";

            var queryDefinition = new QueryDefinition(sqlQueryText);
            var queryResultSetIterator = _container.GetItemQueryIterator<CosmosEntities.ConferenceEntity>(queryDefinition);

            var sequenceNumber = 0;

            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (var document in currentResultSet)
                {
                    sequenceNumber = document.SequenceNumber;
                }
            }

            return sequenceNumber + 1;
        }

        public async Task<ItemResponse<CosmosEntities.ConferenceEntity>> InsertAsync(ConferenceModel model)
        {
            var streamId = GetConferenceId(model);

            var sequenceNumber = await GetNextAsync(streamId);

            var entity = new CosmosEntities.ConferenceEntity()
            {
                PartitionKey = streamId,
                SequenceNumber = sequenceNumber,
                ConferenceModel = model
            };

            var response = await _container.CreateItemAsync(entity);

            return response;
        }
    }
}
