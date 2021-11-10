using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.API.Web.Models;
using System;
using System.Threading.Tasks;

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
            var cacheStatus = await Core.Data.CacheManager.GetCacheStatus(false, false, true);

            return
             new SystemInfoResult
             {
                 SystemVersion = "3",
                 POIDataLastModified = cacheStatus.LastPOIUpdate.GetValueOrDefault(),
                 POIDataLastCreated = cacheStatus.LastPOICreated.GetValueOrDefault(),
                 MaxPOIId = cacheStatus.MaxPOIId,
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


        private User GetUserFromAPIKey()
        {
            if (HttpContext.Request.Headers.ContainsKey("X-API-Key"))
            {
                var apiKey = HttpContext.Request.Headers["X-API-Key"];
                if (!string.IsNullOrEmpty(apiKey))
                {
                    var user = new UserManager().GetUserFromAPIKey(apiKey);
                    return user;
                }
            }
            return null;
        }

        [HttpGet]
        [Route("importcompleted/{id}")]
        [Route("/v3/system/importcompleted/{id}")]
        public async Task<IActionResult> UpdateProviderImport(int id)
        {
            var user = GetUserFromAPIKey();

            if (user != null)

            {
                if (user.Identifier.ToLower() == "system")
                {
                    new DataProviderManager().UpdateDateLastImport(id);
                    await Core.Data.CacheManager.RefreshCachedData(Core.Data.CacheUpdateStrategy.Modified);
                }
            }

            return new OkResult();
        }

#if DEBUG
        [HttpGet]
        [Route("cacherefresh")]
        public async Task<Core.Data.MirrorStatus> PerformCacheRefresh(DateTime? dateModified = null)
        {
            return await Core.Data.CacheManager.RefreshCachedData(Core.Data.CacheUpdateStrategy.Modified);
        }

#endif
    }
}
