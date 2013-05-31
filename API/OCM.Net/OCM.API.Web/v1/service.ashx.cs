using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCM.API.V1
{
    /// <summary>
    /// Summary description for service
    /// </summary>
    public class APIHandlerV1 : APICoreHTTPHandler
    {
        public APIHandlerV1()
        {
            APIBehaviourVersion = 1;
        }
    }
}