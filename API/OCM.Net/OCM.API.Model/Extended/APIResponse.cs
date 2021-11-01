using System;

namespace OCM.API.Common.Model.Extended
{
    public class APIResponseMetadata
    {
        public int StatusCode { get; set; }
    }

    public class APIResponseEnvelope
    {
        public Object Data { get; set; }
        public APIResponseMetadata Metadata { get; set; }

        public APIResponseEnvelope()
        {
            this.Metadata = new APIResponseMetadata();
            this.Metadata.StatusCode = 200;
        }
    }
}
