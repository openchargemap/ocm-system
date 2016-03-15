using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCM.API.V3.CompactPOI
{
    /// <summary>
    /// Summary description for service
    /// </summary>
    public class APIEndpoint : APICoreHTTPHandler
    {
        public APIEndpoint()
        {
            this.APIBehaviourVersion = 3;
            this.DefaultAction = "getcompactpoilist";
        }
    }
}