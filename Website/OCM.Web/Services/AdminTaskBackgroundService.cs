using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OCM.Web.Services
{
    public class AdminTaskBackgroundService : BackgroundService
    {
        private readonly ILogger<AdminTaskBackgroundService> _logger;
        private readonly IAdminTaskService _adminTaskService;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

        public AdminTaskBackgroundService(
            ILogger<AdminTaskBackgroundService> logger,
            IAdminTaskService adminTaskService)
        {
            _logger = logger;
            _adminTaskService = adminTaskService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Admin Task Background Service started. Running every {Interval} minutes", _interval.TotalMinutes);

            // Initial delay of 1 minute after startup to allow app initialization
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Executing periodic admin tasks at {Time}", DateTime.UtcNow);

                    var result = await _adminTaskService.ExecutePeriodicTasksAsync();

                    _logger.LogInformation(
                        "Admin tasks completed. Notifications: {Notifications}, Auto-approved: {AutoApproved}, Exceptions: {Exceptions}",
                        result.NotificationsSent,
                        result.ItemsAutoApproved,
                        result.ExceptionCount
                    );

                    if (result.LogItems.Count > 0)
                    {
                        foreach (var logItem in result.LogItems)
                        {
                            _logger.LogWarning("Admin task log: {LogItem}", logItem);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing periodic admin tasks");
                }

                // Wait for the next interval
                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Admin Task Background Service is stopping");
        }
    }
}
