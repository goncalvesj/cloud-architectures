using System;

namespace EventSourcing.Common
{
    public class ConferenceModel
    {
        public string Event { get; set; }
        public ConferenceDataModel Data { get; set; }
    }

    public class ConferenceDataModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Seats { get; set; }
    }
}