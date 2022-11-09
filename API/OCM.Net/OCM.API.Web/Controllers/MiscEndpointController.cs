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

        [HttpGet]
        [Route("/v3/openapi")]
        public IActionResult GetOpenAPIDefinition()
        {
            return Redirect("https://raw.githubusercontent.com/openchargemap/ocm-docs/master/Model/schema/ocm-openapi-spec.yaml");
        }
    }
}
