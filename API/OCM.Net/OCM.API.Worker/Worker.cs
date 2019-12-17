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
        private Timer? _timer;
        private bool _isSyncInProgress = false;
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

        private async void PerformTasks(object? state)
        {
            if (!_isSyncInProgress)
            {
                _isSyncInProgress = true;
                _logger.LogInformation("Checking sync status..");

                try
                {
                    var sync = new Core.Data.MirrorStatus { NumPOILastUpdated = 0, MaxBatchSize = 0 };

                    while (sync.NumPOILastUpdated == sync.MaxBatchSize)
                    {
                        sync = await Core.Data.CacheManager.RefreshCachedData(Core.Data.CacheUpdateStrategy.Modified, _logger);
                        if (sync.StatusCode == System.Net.HttpStatusCode.ExpectationFailed)
                        {
                            _logger.LogInformation("POI Update failed::" + sync.Description);
                            break;
                        }
                        else
                        {
                            _logger.LogInformation($"Latest POI update :: {sync.NumPOILastUpdated} POIs {sync.TotalPOIInCache} in total {sync.LastPOIUpdate}");
                        }
                    }

                }
                catch (Exception exp)
                {
                    _logger.LogWarning("POI update failed::" + exp.ToString());
                }
                _isSyncInProgress = false;
            }
            else
            {
                _logger.LogWarning("POI update :: Previous sync still in progress");
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
