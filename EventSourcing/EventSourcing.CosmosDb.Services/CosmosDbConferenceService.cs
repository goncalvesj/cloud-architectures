using System;
using System.Threading.Tasks;
using EventSourcing.Common;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace EventSourcing.CosmosDb.Services
{
    public interface IConferenceCosmosDbService
    {
        Task<ItemResponse<CosmosEntities.ConferenceEntity>> InsertAsync(ConferenceModel model);
    }

    public class CosmosDbConferenceService : IConferenceCosmosDbService
    {
        private readonly Container _container;

        public CosmosDbConferenceService(IOptions<Settings.CosmosSettings> options)
        {
            var cosmosSettings = options.Value;

            var client = new CosmosClient(cosmosSettings.Endpoint, cosmosSettings.AuthKey);
            _container = client.GetContainer(cosmosSettings.CosmosDatabaseId, cosmosSettings.ContainerId);
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
