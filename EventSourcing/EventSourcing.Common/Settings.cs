using System;
using System.Collections.Generic;
using System.Text;

namespace EventSourcing.Common
{
    public class Settings
    {
        public class CosmosSettings
        {
            public string CosmosDatabaseId { get; set; }
            public string ContainerId { get; set; }
            public string Endpoint { get; set; }
            public string AuthKey { get; set; }
        }
    }
}
