using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OCM.API.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private Timer _timer;
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("API Worker Starting.");

            // check for work to do every 5 minutes
            _timer = new Timer(PerformTasks, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5));


            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000 * 60 * 5, stoppingToken);
            }
        }

        private async void PerformTasks(object state)
        {
            _logger.LogInformation("Checking sync status..");

            try
            {
                var refresh = await Core.Data.CacheManager.RefreshCachedData(Core.Data.CacheUpdateStrategy.Modified);

                if (refresh.StatusCode== System.Net.HttpStatusCode.ExpectationFailed)
                {
                    _logger.LogInformation("POI Update failed::" + refresh.Description);
                } else
                {
                    _logger.LogInformation("Latest POI update::" + refresh.LastPOIUpdate);
                }
                
            }
            catch (Exception exp)
            {
                _logger.LogWarning("POI update failed::" + exp.ToString());
            }
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _timer?.Dispose();

            base.Dispose();
        }
    }
}
