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

    public class MiscEndpointController : ControllerBase
    {
        private readonly ILogger _logger;

        public MiscEndpointController(ILogger<MiscEndpointController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("/map")]
        public IActionResult Get()
        {
            return Redirect("https://map.openchargemap.io");
        }
    }
}
