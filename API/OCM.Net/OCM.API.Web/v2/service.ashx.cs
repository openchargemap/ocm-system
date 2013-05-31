using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCM.API.V2
{
    /// <summary>
    /// Summary description for service
    /// </summary>
    public class APIHandler : APICoreHTTPHandler
    {
        public APIHandler()
        {
            this.APIBehaviourVersion = 2;
            this.DefaultAction = "nop"; //do nothing for root level request
        }
    }
}