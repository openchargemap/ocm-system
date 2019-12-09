using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.API.Web.Models;

namespace OCM.API.Web.Standard.Controllers
{
    [ApiController]
    [Route("/v4/[controller]")]
    public class SystemController : ControllerBase
    {
        private readonly ILogger _logger;

        public SystemController(ILogger<SystemController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public Task<SystemInfoResult> Get()
        {
            return Task.FromResult(
             new SystemInfoResult
             {
                 SystemVersion = "3",
                 DataVersionHash = Guid.NewGuid().ToString(),
                 DataVersionTimestamp = DateTime.UtcNow.Ticks.ToString()
             });
        }
    }
}
