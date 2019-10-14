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
    [Route("/api/[controller]")]
    public class POIController : ControllerBase
    {
        private readonly ILogger<POIController> _logger;

        public POIController(ILogger<POIController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<ChargePoint> Get()
        {
            var api = new POIManager();
            
            var list = api.GetChargePoints(new APIRequestParams { });

            return list;
        }
    }
}
