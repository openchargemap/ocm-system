using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OCM.Core.Data;
using System.Linq;

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

        [HttpGet]
        [Route("/v3/key")]
        public IActionResult CheckKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return NotFound();
            }

            var result = new OCMEntities().RegisteredApplications.Where(a => a.PrimaryApikey == key.ToLower() && a.IsEnabled).FirstOrDefault();

            if (result != null)
            {
                var o = new
                {
                    AppId = result.AppId,
                    Title = result.Title,
                    Url = result.WebsiteUrl
                };

                return Ok(o);
            }
            else
            {
                return NotFound();
            }
        }
    }
}
