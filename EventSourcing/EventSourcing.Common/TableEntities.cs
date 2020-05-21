using Microsoft.Azure.Cosmos.Table;

namespace EventSourcing.Common
{
    public class TableEntities
    {
        public class EventStoreEntity : TableEntity
        {
            public EventStoreEntity()
            {
            }

            public EventStoreEntity(string eventStream, string sequenceNumber)
            {
                PartitionKey = eventStream;
                RowKey = sequenceNumber;
            }

            public string EventType { get; set; }
            public string Payload { get; set; }
        }

        public class EventProjectionsEntity : TableEntity
        {
            public EventProjectionsEntity()
            {
            }

            public EventProjectionsEntity(string eventStream, string id)
            {
                PartitionKey = eventStream;
                RowKey = id;
            }
            public string LastSequenceRun { get; set; }
            public string Payload { get; set; }
        }
    }
}