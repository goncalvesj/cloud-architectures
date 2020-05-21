namespace EventSourcing.Common
{
    public class QueueEntities
    {
        public class Message
        {
            public string Stream { get; set; }
            public string SequenceNumber { get; set; }
            public string Id { get; set; }
        }
    }
}