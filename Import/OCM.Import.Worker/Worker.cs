using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCM.Import.Providers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OCM.Import.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private Timer? _timer;
        private bool _isImportInProgress = false;
        private ImportSettings _settings;
        private IConfiguration _config;
        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _config = configuration;


            _settings = new ImportSettings();
            configuration.GetSection("ImportSettings").Bind(_settings);


            if (string.IsNullOrEmpty(_settings.ImportUserAgent)) _settings.ImportUserAgent = "OCM-API-Worker";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Import Worker Starting. Import syncing every {_settings.ImportRunFrequencyMinutes} minutes");

            // check for work to do every N minutes
            _timer = new Timer(PerformTasks, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(_settings.ImportRunFrequencyMinutes*60));
        }

        private async void PerformTasks(object? state)
        {
            if (!_isImportInProgress)
            {
                _isImportInProgress = true;
                _logger.LogInformation("Checking import status..");

                var tempPath = Path.GetTempPath();
                var importManager = new ImportManager(_settings, _logger);

                try
                {
                    var status = new ImportStatus { DateLastImport = DateTime.UtcNow, LastImportedProvider = "", LastImportStatus = "Started" };

                    var statusPath = Path.Combine(tempPath, "import_status.json");
                    if (File.Exists(statusPath))
                    {
                        status = System.Text.Json.JsonSerializer.Deserialize<ImportStatus>(File.ReadAllText(statusPath));
                    }


                    var allProviders = _settings.EnabledImports;
                    var indexOfLastProvider = allProviders.IndexOf(status.LastImportedProvider ?? "");
                    var indexOfNextProvider = 0;

                    // if there is a next provider use that, otherwise use first provider
                    if (indexOfLastProvider > -1)
                    {
                        if (indexOfLastProvider < allProviders.Count - 1)
                        {
                            // next provider
                            indexOfNextProvider = indexOfLastProvider + 1;
                        }
                    }
                    else
                    {
                        indexOfNextProvider = 0;
                    }

                    status.LastImportedProvider = allProviders[indexOfNextProvider];

                    status.DateLastImport = DateTime.UtcNow;

                    _logger.LogInformation($"Performing Import [{status.LastImportedProvider}], Publishing via API: {status.DateLastImport.Value.ToShortTimeString()}");

                    try
                    {
                        var stopwatch = Stopwatch.StartNew();

                        var apiKeys = _config.AsEnumerable().Where(k => k.Key.StartsWith("OCPI-") || k.Key.StartsWith("IMPORT-")).ToDictionary(k => k.Key, v => v.Value);

                        var importedOK = await importManager.PerformImportProcessing(
                            new ImportProcessSettings
                            {
                                ExportType = Providers.ExportType.JSONAPI,
                                DefaultDataPath = tempPath,
                                FetchLiveData = true,
                                FetchExistingFromAPI = true,
                                PerformDeduplication = true,
                                ProviderName = status.LastImportedProvider, // leave blank to do all
                                Credentials = apiKeys
                            });

                        status.LastImportStatus = importedOK ? "Imported" : "Failed";
                        status.DateLastImport = DateTime.UtcNow;
                        status.ProcessingTimeSeconds = stopwatch.Elapsed.TotalSeconds;

                        // update import date on provider
                        var providers = importManager.GetImportProviders(new List<API.Common.Model.DataProvider>());
                        var providerId = (providers.FirstOrDefault(p => p.GetProviderName() == status.LastImportedProvider) as BaseImportProvider)?.DataProviderID;
                        if (providerId != null)
                        {
                            //  notify API of last date of import for each provider
                            await importManager.UpdateLastImportDate((int)providerId);
                        }

                    }
                    catch (Exception exp)
                    {
                        _logger.LogError("Import failed: " + exp);

                        status.LastImportStatus = "Failed with unknown exception";
                        _timer.Change(2, _settings.ImportRunFrequencyMinutes);
                    }

                    File.WriteAllText(statusPath, System.Text.Json.JsonSerializer.Serialize<ImportStatus>(status));

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
