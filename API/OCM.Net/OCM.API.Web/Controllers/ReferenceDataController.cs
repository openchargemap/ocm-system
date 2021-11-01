using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OCM.API.Common;
using OCM.API.Common.Model;

namespace OCM.API.Web.Standard.Controllers
{
    [ApiController]
    [Route("/v4/[controller]")]
    public class ReferenceDataController : ControllerBase
    {
        private readonly ILogger _logger;

        public ReferenceDataController(ILogger<ReferenceDataController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public CoreReferenceData Get([FromQuery] APIRequestParams filter)
        {
            using (var refData = new ReferenceDataManager())
            {
                return refData.GetCoreReferenceData(filter);
            }
        }

    }
}
