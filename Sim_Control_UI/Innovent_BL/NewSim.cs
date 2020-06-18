using Newtonsoft.Json;
using System.Collections.Generic;

namespace Innovent_BL
{

    public class Data
    {
        [JsonProperty("sims")]
        public Sims Sims { get; set; }
    }

    public class Sims
    {
        [JsonProperty("edges")]
        public List<Edge> Edges { get; set; }
    }

    public class Edge
    {
        [JsonProperty("node")]
        public Node Node { get; set; }
    }

    public class Node
    {
        [JsonProperty("contactNumber")]
        public string ContactNumber { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("network")]
        public Network Network { get; set; }

        [JsonProperty("airtimeBalance")]
        public long? AirtimeBalance { get; set; }

        [JsonProperty("dataBalanceInMb")]
        public long? DataBalanceInMb { get; set; }

        [JsonProperty("smsBalance")]
        public long? SmsBalance { get; set; }
    }

    public class Network
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
