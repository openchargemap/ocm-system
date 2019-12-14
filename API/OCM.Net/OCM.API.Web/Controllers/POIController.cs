using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OCM.API.Common;
using OCM.API.Common.Model;

namespace OCM.API.Web.Standard.Controllers
{
    [ApiController]
    [Route("/v4/[controller]")]
    public class POIController : ControllerBase
    {
        private readonly ILogger _logger;

        public POIController(ILogger<POIController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<ChargePoint> Get()
        {

            // use custom query string parsing for compatibility
            var filter = new APIRequestParams();

            //set defaults
            var paramList = new NullSafeDictionary<string, string>();
            foreach (var k in Request.Query.Keys)
            {
                paramList.Add(k.ToLower(), Request.Query[k]);
            }

            filter.ParseParameters(filter, paramList);

            if (string.IsNullOrEmpty(filter.APIKey))
            {
                if (Request.Headers.ContainsKey("X-API-Key"))
                {
                    filter.APIKey = Request.Headers["X-API-Key"];
                }
            }

            var api = new POIManager();

            var list = api.GetPOIList(filter);

            return list;
        }
    }
}
