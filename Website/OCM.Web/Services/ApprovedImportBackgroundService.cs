using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCM.API.Common.Model;

namespace OCM.Web.Services
{
    public class ApprovedImportBackgroundService : BackgroundService
    {
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan Interval = TimeSpan.FromHours(12);

        private readonly IImportQueueService _importQueueService;
        private readonly ILogger<ApprovedImportBackgroundService> _logger;

        public ApprovedImportBackgroundService(
            IImportQueueService importQueueService,
            ILogger<ApprovedImportBackgroundService> logger)
        {
            _importQueueService = importQueueService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Approved import background service started. Running every {IntervalHours} hours.", Interval.TotalHours);

            await Task.Delay(StartupDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var jobs = _importQueueService.QueueApprovedImports((int)StandardUsers.System);
                    var activeOrQueuedCount = jobs.Count(job => job.IsActive);

                    _logger.LogInformation(
                        "Queued scheduled approved imports at {Time}. Agreements discovered: {AgreementCount}, active or queued jobs: {ActiveJobCount}.",
                        DateTime.UtcNow,
                        jobs.Count,
                        activeOrQueuedCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error queueing scheduled approved imports.");
                }

                await Task.Delay(Interval, stoppingToken);
            }

            _logger.LogInformation("Approved import background service is stopping.");
        }
    }
}
