using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.Model.OCPI
{
    public class LocationsResponse
    {
        [Newtonsoft.Json.JsonProperty("data")]
        public List<Location> Data { get; set; }

        [Newtonsoft.Json.JsonProperty("status_code")]
        public int StatusCode { get; set; }

        [Newtonsoft.Json.JsonProperty("status_message")]
        public string StatusMessage { get; set; }

        [Newtonsoft.Json.JsonProperty("timestamp")]
        public string Timestamp { get; set; }
    }
}
