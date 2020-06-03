using System;
using EventSourcing.Common;
using Newtonsoft.Json;

namespace EventSourcing.CosmosDb.Services
{
    public class CosmosEntities
    {
        public class ConferenceEntity
        {
            public ConferenceEntity()
            {
                Id = Guid.NewGuid().ToString();
            }

            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
            [JsonProperty(PropertyName = "partitionKey")]
            public string PartitionKey { get; set; }
            [JsonProperty(PropertyName = "sequenceNumber")]
            public int SequenceNumber { get; set; }
            public ConferenceModel ConferenceModel { get; set; }
        }

        public class ConferenceProjectionEntity
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
            [JsonProperty(PropertyName = "partitionKey")]
            public string PartitionKey { get; set; }
            public ConferenceDataModel ConferenceDataModel { get; set; }
        }
    }
}
