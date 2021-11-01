using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
