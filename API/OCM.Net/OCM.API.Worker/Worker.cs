using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCM.Core.Settings;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OCM.API.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private Timer? _timer;
        private bool _isSyncInProgress = false;
        private CoreSettings _settings;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;

            _settings = new CoreSettings();
            configuration.GetSection("CoreSettings").Bind(_settings);

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"API Worker Starting. Cache syncing every {_settings.MongoDBSettings.CacheSyncFrequencySeconds}s");

            // check for work to do every N seconds
            _timer = new Timer(PerformTasks, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(_settings.MongoDBSettings.CacheSyncFrequencySeconds));
        }

        private async void PerformTasks(object? state)
        {
            if (!_isSyncInProgress)
            {
                _isSyncInProgress = true;
                _logger.LogInformation("Checking sync status..");

                try
                {
                    var sync = new Core.Data.MirrorStatus { NumPOILastUpdated = -1, MaxBatchSize = -1 };

                    while (sync.NumPOILastUpdated == sync.MaxBatchSize && sync.NumPOILastUpdated != 0)
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
