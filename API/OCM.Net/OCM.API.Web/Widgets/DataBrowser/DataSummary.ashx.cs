using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;
using System.Threading;
using OCM.API.Common;
using OCM.API.Common.DataSummary;
using System.Runtime.Serialization.Json;
using OCM.API.OutputProviders;
using Newtonsoft.Json;

namespace OCM.API.Widgets.DataBrowser
{
    /// <summary>
    /// Summary description for DataSummary
    /// </summary>
    public class DataSummary : ServiceParameterParser, IHttpHandler
    {
        public DataSummaryManager dataSummaryManager = null;

        public void ProcessRequest(HttpContext context)
        {
            if (dataSummaryManager == null) dataSummaryManager = new DataSummaryManager();

            APIRequestParams filterSettings = new APIRequestParams();
            filterSettings.ParseParameters(filterSettings,context);

            string action = "totals_per_country";

            if (!String.IsNullOrEmpty(context.Request["action"]))
            {
                action = context.Request["action"].ToString();
            }

            if (action == "totals_per_country")
            {
                context.Response.ContentType = "application/javascript";
                context.Response.Write(dataSummaryManager.GetTotalsPerCountrySummary(true,"ocm_getdatasummary", filterSettings ));
                context.Response.Flush();
            }

            if (action == "full_summary")
            {
                // Output JSON summary of:
                // - Current totals per country
                // - Total added (per country? based on date range, location/distance filter?)
                // - Total modified
                // - User Comments Count

                // - per month values, current month first? default last 12 months
            }

            if (action == "activity_summary")
            {
                // Based on date range, location and distance:
                // - list of recent comments, checkins, id & title etc of items added & modified
                var o = new JSONOutputProvider();

                context.Response.ContentType = o.ContentType;
                var summary = dataSummaryManager.GetActivitySummary(filterSettings);
                o.PerformSerialisationV2(context.Response.OutputStream, summary, filterSettings.Callback);
                
                context.Response.Flush();
            }
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}