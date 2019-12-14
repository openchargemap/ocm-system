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
        [Route("status")]
        [Route("/v3/system/status")]
        public async Task<SystemInfoResult> GetStatus()
        {
            var cacheStatus = await Core.Data.CacheManager.GetCacheStatus(false, true, true);

            return
             new SystemInfoResult
             {
                 SystemVersion = "3",
                 POIDataLastModified = cacheStatus.LastPOIUpdate.Value,
                 POIDataLastCreated = cacheStatus.LastPOICreated.Value,
                 DataHash = cacheStatus.ContentHash
             };
        }

        [HttpGet]
        [Route("sync")]
        [Route("/v3/system/sync")]
        public async Task<SyncResult> GetSyncData(DateTime? dateModified)
        {
            var result = new SyncResult
            {
                SystemInfo = await this.GetStatus(),
                UpdatedPOIs = await new POIManager().GetPOIListAsync(new APIRequestParams { AllowMirrorDB = true, AllowDataStoreDB = false, ChangesFromDate = dateModified ?? DateTime.UtcNow.AddMonths(-1) }),
                ReferenceData = new ReferenceDataManager().GetCoreReferenceData(new APIRequestParams { AllowMirrorDB = true, AllowDataStoreDB = false, ChangesFromDate = DateTime.UtcNow.AddMonths(-6) })
            };

            return result;
        }

#if DEBUG
        [HttpGet]
        [Route("cacherefresh")]
        public async Task<Core.Data.MirrorStatus> PerformCacheRefresh(DateTime? dateModified)
        {
            return await Core.Data.CacheManager.RefreshCachedData(Core.Data.CacheUpdateStrategy.Modified);
        }

#endif
    }
}
