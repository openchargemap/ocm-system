using System.Collections.Generic;

namespace OCM.Model.OCPI
{
    /// <summary>
    /// Hand written additions to the NSwag generated <see cref="Location"/> model.
    /// </summary>
    public partial class Location
    {
        /// <summary>
        /// Treat a location as published unless the feed says otherwise. OCPI 2.1.1 has no publish
        /// field and some later feeds omit it, and the generated property is a non-nullable bool, so
        /// without this default every location in such a feed would look unpublished.
        /// </summary>
        [System.Runtime.Serialization.OnDeserializing]
        internal void SetDefaultsBeforeDeserializing(System.Runtime.Serialization.StreamingContext context)
        {
            Publish = true;
        }
    }

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
