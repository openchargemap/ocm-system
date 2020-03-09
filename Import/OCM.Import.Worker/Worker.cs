using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OCM.Import.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private Timer? _timer;
        private bool _isImportInProgress = false;
        private ImportSettings _settings;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;

            _settings = new ImportSettings();
            configuration.GetSection("ImportSettings").Bind(_settings);
            if (string.IsNullOrEmpty(_settings.ImportUserAgent)) _settings.ImportUserAgent = "OCM-API-Worker";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Import Worker Starting. Import syncing every {_settings.ImportRunFrequencyMinutes} minutes");

            // check for work to do every N minutes
            _timer = new Timer(PerformTasks, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(_settings.ImportRunFrequencyMinutes));
        }

        private async void PerformTasks(object? state)
        {
            if (!_isImportInProgress)
            {
                _isImportInProgress = true;
                _logger.LogInformation("Checking import status..");

                var tempPath = Path.GetTempPath();
                var importManager = new ImportManager(_settings);

                try
                {
                    _ = await importManager.PerformImportProcessing(Providers.ExportType.JSONAPI, tempPath, null, null, true, true, "mobie.pt");
                }
                catch (Exception exp)
                {
                    _logger.LogError("Import failed: " + exp.ToString());
                }
                _isImportInProgress = false;

            }
            else
            {
                _logger.LogInformation("Import already in progress..");
            }

        }
    }
}
