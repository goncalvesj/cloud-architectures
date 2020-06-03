using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Common;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;

namespace EventSourcing.Table.Services
{
    public interface ITableProjectionService
    {
    }

    public class TableProjectionService : ITableProjectionService
    {
        private readonly CloudTable _eventProjectionsTable;
        private readonly CloudTable _eventStoreTable;

        public TableProjectionService()
        {
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());

            _eventProjectionsTable = tableClient.GetTableReference("EventProjectionsTable");
            _eventProjectionsTable.CreateIfNotExists();

            _eventStoreTable = tableClient.GetTableReference("EventStoreTable");
            _eventStoreTable.CreateIfNotExists();
        }
        
        public ConferenceDataModel GetConferenceDetails(string conferenceId)
        {
            var linqQuery = _eventProjectionsTable.CreateQuery<TableEntities.EventProjectionsEntity>()
                .Where(x => x.PartitionKey == "Conference" && x.RowKey == conferenceId)
                .Select(x => x.Payload)
                .SingleOrDefault();

            var response = JsonConvert.DeserializeObject<ConferenceDataModel>(linqQuery);

            return response;
        }

        public List<ConferenceDataModel> GetAllConferences()
        {
            var linqQuery = _eventProjectionsTable.CreateQuery<TableEntities.EventProjectionsEntity>()
                .Where(x => x.PartitionKey == "LookUps" && x.RowKey == "AllConferences")
                .Select(x => x.Payload)
                .SingleOrDefault();

            var response = JsonConvert.DeserializeObject<List<ConferenceDataModel>>(linqQuery);

            return response;
        }

        public async Task BuildProjection(QueueEntities.Message message)
        {
            var linqQuery = _eventStoreTable.CreateQuery<TableEntities.EventStoreEntity>()
                .Where(x => x.PartitionKey == message.Id && x.RowKey == message.SequenceNumber)
                .ToList();

            var entity = message.Stream switch
            {
                "Conference" => CreateConferenceProjection(_eventProjectionsTable, message, linqQuery),
                _ => null
            };

            var insertOrMergeOperation = TableOperation.InsertOrReplace(entity);

            await _eventProjectionsTable.ExecuteAsync(insertOrMergeOperation);

            await UpdateConferenceLookUpProjectionAsync(_eventProjectionsTable, linqQuery.SingleOrDefault());
        }

        public async Task RebuildProjections()
        {
            var linqQuery = _eventStoreTable.CreateQuery<TableEntities.EventStoreEntity>().ToList();

            var partitions = linqQuery.GroupBy(x => x.PartitionKey).Select(x => x.Key);

            var conferenceList = new List<ConferenceDataModel>();
            TableOperation insertOrMergeOperation;

            foreach (var partition in partitions)
            {
                var events = linqQuery
                    .Where(x => x.PartitionKey == partition)
                    .OrderBy(x => x.Timestamp);

                var entity = CreateConferenceProjection(_eventProjectionsTable, new QueueEntities.Message { Stream = "Conference", Id = partition }, events);

                conferenceList.Add(JsonConvert.DeserializeObject<ConferenceDataModel>(entity.Payload));

                insertOrMergeOperation = TableOperation.InsertOrReplace(entity);

                await _eventProjectionsTable.ExecuteAsync(insertOrMergeOperation);
            }

            var allConferences = new TableEntities.EventProjectionsEntity
            {
                PartitionKey = "LookUps",
                RowKey = "AllConferences",
                Payload = JsonConvert.SerializeObject(conferenceList)
            };

            insertOrMergeOperation = TableOperation.InsertOrReplace(allConferences);

            await _eventProjectionsTable.ExecuteAsync(insertOrMergeOperation);
        }

        public async Task<TableEntities.EventProjectionsEntity> CreateLookUpProjectionAsync(CloudTable eventProjectionsTable)
        {
            var allConferences = new TableEntities.EventProjectionsEntity
            {
                PartitionKey = "LookUps",
                RowKey = "AllConferences",
                Payload = string.Empty
            };

            var insertOrMergeOperation = TableOperation.InsertOrReplace(allConferences);

            var result = await eventProjectionsTable.ExecuteAsync(insertOrMergeOperation);

            return result.Result as TableEntities.EventProjectionsEntity;
        }

        public async Task UpdateConferenceLookUpProjectionAsync(CloudTable eventProjectionsTable, TableEntities.EventStoreEntity entity)
        {
            var projection = eventProjectionsTable
                .CreateQuery<TableEntities.EventProjectionsEntity>()
                .Where(x => x.PartitionKey == "LookUps" && x.RowKey == "AllConferences")
                .Select(x => x)
                .SingleOrDefault() ?? await CreateLookUpProjectionAsync(eventProjectionsTable);

            var data = !string.IsNullOrEmpty(projection.Payload)
                ? JsonConvert.DeserializeObject<List<ConferenceDataModel>>(projection.Payload)
                : new List<ConferenceDataModel>();

            var eventData = JsonConvert.DeserializeObject<ConferenceDataModel>(entity?.Payload);

            var conferenceData = data.SingleOrDefault(x => x.Id == eventData.Id);

            switch (entity?.EventType)
            {
                case "Conference.Created":
                    data.Add(eventData);
                    break;
                case "Conference.SeatsAdded":
                    if (conferenceData != null) conferenceData.Seats += eventData.Seats;
                    break;
                case "Conference.SeatsRemoved":
                    if (conferenceData != null) conferenceData.Seats -= eventData.Seats;
                    break;
            }

            projection.Payload = JsonConvert.SerializeObject(data);

            var insertOrMergeOperation = TableOperation.InsertOrReplace(projection);

            await eventProjectionsTable.ExecuteAsync(insertOrMergeOperation);
        }

        public TableEntities.EventProjectionsEntity CreateConferenceProjection(CloudTable eventProjectionsTable, QueueEntities.Message message, IEnumerable<TableEntities.EventStoreEntity> list)
        {
            var projection = eventProjectionsTable
                .CreateQuery<TableEntities.EventProjectionsEntity>()
                .Where(x => x.PartitionKey == "Conference" && x.RowKey == message.Id)
                .Select(x => x)
                .SingleOrDefault() ?? new TableEntities.EventProjectionsEntity(message.Stream, message.Id);

            var dataModel = !string.IsNullOrEmpty(projection.Payload)
            ? JsonConvert.DeserializeObject<ConferenceDataModel>(projection.Payload)
            : new ConferenceDataModel();

            var lastSequenceRun = string.Empty;

            foreach (var item in list.OrderBy(x => x.Timestamp))
            {
                lastSequenceRun = item.RowKey;
                ConferenceDataModel data;

                switch (item.EventType)
                {
                    case "Conference.Created":
                        dataModel = JsonConvert.DeserializeObject<ConferenceDataModel>(item.Payload);
                        break;
                    case "Conference.SeatsAdded":
                        data = JsonConvert.DeserializeObject<ConferenceDataModel>(item.Payload);
                        dataModel.Seats += data.Seats;
                        break;
                    case "Conference.SeatsRemoved":
                        data = JsonConvert.DeserializeObject<ConferenceDataModel>(item.Payload);
                        dataModel.Seats -= data.Seats;
                        break;
                }
            }

            var entity = new TableEntities.EventProjectionsEntity(message.Stream, message.Id)
            {
                LastSequenceRun = lastSequenceRun,
                Payload = JsonConvert.SerializeObject(dataModel)
            };

            return entity;
        }
    }
}
