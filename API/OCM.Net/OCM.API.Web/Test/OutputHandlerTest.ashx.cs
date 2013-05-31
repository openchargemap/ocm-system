using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OCM.API.Common;

namespace OCM.API.Test
{
    /// <summary>
    /// Summary description for OutputHandlerTest
    /// </summary>
    public class OutputHandlerTest : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            OutputProviders.ImageOutputProvider outputProvider = new OutputProviders.ImageOutputProvider();
            SearchFilterSettings settings = new SearchFilterSettings();

            List<OCM.API.Common.Model.ChargePoint> dataList = new List<Common.Model.ChargePoint>();

            context.Response.ContentType = outputProvider.ContentType;
            outputProvider.GetOutput(context.Response.OutputStream, dataList, settings);
            
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}