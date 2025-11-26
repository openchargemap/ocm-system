using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.Core.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OCM.Web.Services
{
    public class AdminTaskService : IAdminTaskService
    {
        private readonly IWebHostEnvironment _host;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminTaskService> _logger;

        public AdminTaskService(
            IWebHostEnvironment host,
            IMemoryCache memoryCache,
            IConfiguration configuration,
            ILogger<AdminTaskService> logger)
        {
            _host = host;
            _cache = memoryCache;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AdminTaskResult> ExecutePeriodicTasksAsync()
        {
            var result = new AdminTaskResult();

            try
            {
                _logger.LogInformation("Starting periodic admin tasks execution");

                // Send all pending subscription notifications
                var enableSubscriptionChecks = _configuration.GetValue<bool>("EnableSubscriptionChecks", false);
                if (enableSubscriptionChecks)
                {
                    try
                    {
                        string templateFolderPath = _host.ContentRootPath + "/templates/Notifications";
                        result.NotificationsSent = await new UserSubscriptionManager().SendAllPendingSubscriptionNotifications(templateFolderPath);
                        _logger.LogInformation($"Sent {result.NotificationsSent} subscription notifications");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send subscription notifications");
                        result.ExceptionCount++;
                    }
                }

                var autoApproveDays = 3;
                var poiManager = new POIManager();
                var userManager = new UserManager();
                var systemUser = userManager.GetUser((int)StandardUsers.System);

                // Check for edit queue items to auto approve
                try
                {
                    // Pre-evaluate the cutoff date to avoid expression translation issues
                    var editQueueCutoffDate = DateTime.UtcNow.AddDays(-autoApproveDays);

                    using (var editQueueManager = new EditQueueManager())
                    {
                        var queueItems = (await editQueueManager.GetEditQueueItems(new EditQueueFilter { ShowProcessed = false, ShowEditsOnly = false }))
                            .Where(q => q.DateProcessed == null).ToList()
                            .OrderBy(q => q.DateSubmitted);

                        foreach (var i in queueItems)
                        {
                            try
                            {
                                var editPOI = JsonConvert.DeserializeObject<OCM.API.Common.Model.ChargePoint>(i.EditData);
                                var submitter = userManager.GetUser(i.User.ID);

                                var submitterHasEditPermissions = UserManager.HasUserPermission(submitter, editPOI.AddressInfo.CountryID, PermissionLevel.Editor);

                                // Auto approve if submitter has edit permissions, or edit has been in queue for more than 3 days
                                if (submitterHasEditPermissions || i.DateSubmitted < editQueueCutoffDate)
                                {
                                    await editQueueManager.ProcessEditQueueItem(i.ID, true, (int)StandardUsers.System, false, "Auto Approved");
                                    result.ItemsAutoApproved++;
                                }
                                else
                                {
                                    if (submitter.ReputationPoints >= 50 && submitter.Permissions == null)
                                    {
                                        result.LogItems.Add($"User {submitter.Username} [{submitter.ID}] has high reputation [{submitter.ReputationPoints}] but no country editor permissions.");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing edit queue item {ItemId}", i.ID);
                                result.ExceptionCount++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching or processing edit queue items");
                    result.ExceptionCount++;
                }

                // Auto approve new items if they have been in the system for more than 3 days
                try
                {
                    // Evaluate the cutoff date outside the query to avoid MongoDB translation issues
                    var cutoffDate = DateTime.UtcNow.AddDays(-autoApproveDays);
                    var submittedUnderReview = (int)StandardSubmissionStatusTypes.Submitted_UnderReview;
                    var importedUnderReview = (int)StandardSubmissionStatusTypes.Imported_UnderReview;
                    var submittedPublished = (int)StandardSubmissionStatusTypes.Submitted_Published;

                    var newPois = await poiManager.GetPOIListAsync(new APIRequestParams { SubmissionStatusTypeID = new int[] { 1 } });

                    var list = newPois.ToList();

                    foreach (var poi in list)
                    {
                        if (poi.SubmissionStatusTypeID == submittedUnderReview ||
                            poi.SubmissionStatusTypeID == importedUnderReview)
                        {
                            if (poi.DateCreated < cutoffDate)
                            {
                                try
                                {
                                    poi.SubmissionStatusTypeID = submittedPublished;
                                    await new SubmissionManager().PerformPOISubmission(poi, systemUser);
                                    result.ItemsAutoApproved++;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error auto-approving POI {PoiId}", poi.ID);
                                    result.ExceptionCount++;
                                }
                            }
                        }
                    }

                    _logger.LogInformation($"Auto-approved {result.ItemsAutoApproved} items");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching or processing POIs for auto-approval. ");
                    result.ExceptionCount++;
                }

                // Update cache mirror
                try
                {
                    _cache.Set("_MirrorRefreshInProgress", true);
                    result.MirrorStatus = await CacheManager.RefreshCachedData(CacheUpdateStrategy.Modified);
                    _logger.LogInformation($"Cache mirror updated: {result.MirrorStatus.TotalPOIInCache} POIs in cache");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update cache mirror");
                    result.ExceptionCount++;
                }
                finally
                {
                    _cache.Set("_MirrorRefreshInProgress", false);
                }

                // Update stats
                if (_cache.Get("_StatsRefreshed") == null)
                {
                    try
                    {
                        using (var dataSummaryManager = new API.Common.DataSummary.DataSummaryManager())
                        {
                            await dataSummaryManager.RefreshStats();
                            _cache.Set("_StatsRefreshed", true);
                            _logger.LogInformation("Stats refreshed successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to refresh stats");
                        result.ExceptionCount++;
                    }
                }

                _logger.LogInformation("Completed periodic admin tasks execution");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during periodic admin tasks execution");
                result.ExceptionCount++;
            }

            return result;
        }
    }
}
