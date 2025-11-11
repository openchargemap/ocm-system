using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCM.API.Client;
using OCM.API.Common.Model;

namespace OCM.Import.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private Timer? _timer;
        private bool _isImportInProgress = false;
        private ImportSettings _settings;
        private IConfiguration _config;
        private OCMClient _client;
        private const int IMPORT_DUE_THRESHOLD_HOURS = 24;

        // Track failed imports in memory to prevent retry within threshold
        private readonly Dictionary<string, FailedImportInfo> _failedImports = new Dictionary<string, FailedImportInfo>();
        private readonly object _failedImportsLock = new object();

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _config = configuration;

            _settings = new ImportSettings();
            configuration.GetSection("ImportSettings").Bind(_settings);

            if (string.IsNullOrEmpty(_settings.ImportUserAgent)) _settings.ImportUserAgent = "OCM-Import-Worker";

            // Initialize API client for fetching data provider list
            var apiKeys = _config.AsEnumerable().Where(k => k.Key.StartsWith("OCPI-") || k.Key.StartsWith("IMPORT-")).ToDictionary(k => k.Key, v => v.Value);
            var systemApiKey = apiKeys.ContainsKey("IMPORT-ocm-system") ? apiKeys["IMPORT-ocm-system"] : null;
            _client = new OCMClient(_settings.MasterAPIBaseUrl, systemApiKey, _logger, _settings.ImportUserAgent);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Import Worker Starting. Checking for imports every {_settings.ImportRunFrequencyMinutes} minutes");

            // Perform initial check immediately (after 5 seconds)
            _timer = new Timer(
                async (state) => await PerformTasks(state), 
                null, 
                TimeSpan.FromSeconds(5), 
                TimeSpan.FromMinutes(_settings.ImportRunFrequencyMinutes)
            );

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task PerformTasks(object? state)
        {
            if (_isImportInProgress)
            {
                _logger.LogInformation("Import already in progress, skipping this cycle..");
                return;
            }

            _isImportInProgress = true;

            try
            {
                _logger.LogInformation("Checking for imports due..");

                // Clean up old failed import records (older than threshold)
                CleanupFailedImports();

                // Fetch current data provider list from API
                var coreRefData = await _client.GetCoreReferenceDataAsync();
                var allDataProviders = coreRefData.DataProviders;

                if (allDataProviders == null || !allDataProviders.Any())
                {
                    _logger.LogWarning("No data providers found from API");
                    return;
                }

                // Filter to enabled providers only
                var enabledProviderNames = _settings.EnabledImports ?? new List<string>();
                
                if (!enabledProviderNames.Any())
                {
                    _logger.LogWarning("No enabled imports configured in settings");
                    return;
                }

                // Find providers that are due for import (last imported > 24 hours ago or never imported)
                var now = DateTime.UtcNow;
                var providersDue = allDataProviders
                    .Where(dp => enabledProviderNames.Any(name => 
                        string.Equals(dp.Title, name, StringComparison.OrdinalIgnoreCase)))
                    .Where(dp => 
                        !dp.DateLastImported.HasValue || 
                        (now - dp.DateLastImported.Value).TotalHours >= IMPORT_DUE_THRESHOLD_HOURS)
                    .Where(dp => !IsProviderRecentlyFailed(dp.Title, now)) // Exclude recently failed providers
                    .OrderBy(dp => dp.DateLastImported ?? DateTime.MinValue) // Oldest first
                    .ToList();

                if (!providersDue.Any())
                {
                    // Check if there are providers that would be due but are excluded due to recent failures
                    var providersBlockedByFailure = allDataProviders
                        .Where(dp => enabledProviderNames.Any(name => 
                            string.Equals(dp.Title, name, StringComparison.OrdinalIgnoreCase)))
                        .Where(dp => 
                            !dp.DateLastImported.HasValue || 
                            (now - dp.DateLastImported.Value).TotalHours >= IMPORT_DUE_THRESHOLD_HOURS)
                        .Where(dp => IsProviderRecentlyFailed(dp.Title, now))
                        .ToList();

                    if (providersBlockedByFailure.Any())
                    {
                        _logger.LogInformation(
                            $"{providersBlockedByFailure.Count} provider(s) are due but blocked by recent failures. " +
                            $"They will be retried after the failure threshold expires."
                        );
                    }

                    // No providers due - calculate when the next one will be due
                    var nextProviderDue = allDataProviders
                        .Where(dp => enabledProviderNames.Any(name => 
                            string.Equals(dp.Title, name, StringComparison.OrdinalIgnoreCase)))
                        .Where(dp => dp.DateLastImported.HasValue)
                        .Where(dp => !IsProviderRecentlyFailed(dp.Title, now))
                        .OrderBy(dp => dp.DateLastImported.Value)
                        .FirstOrDefault();

                    if (nextProviderDue != null && nextProviderDue.DateLastImported.HasValue)
                    {
                        var nextDueTime = nextProviderDue.DateLastImported.Value.AddHours(IMPORT_DUE_THRESHOLD_HOURS);
                        var timeUntilDue = nextDueTime - now;
                        
                        _logger.LogInformation(
                            $"No imports currently due. Next provider '{nextProviderDue.Title}' due in {timeUntilDue.TotalHours:F1} hours at {nextDueTime:yyyy-MM-dd HH:mm:ss} UTC"
                        );
                    }
                    else
                    {
                        _logger.LogInformation("No imports currently due");
                    }
                    
                    return;
                }

                // Get the highest priority provider (oldest DateLastImported)
                var providerToImport = providersDue.First();
                var providerName = providerToImport.Title;

                var lastImportAge = providerToImport.DateLastImported.HasValue 
                    ? $"{(now - providerToImport.DateLastImported.Value).TotalHours:F1} hours ago"
                    : "never";

                _logger.LogInformation(
                    $"Performing import for '{providerName}' (ID: {providerToImport.ID}, Last imported: {lastImportAge}). " +
                    $"{providersDue.Count} provider(s) total are due for import."
                );

                var tempPath = !string.IsNullOrEmpty(_settings.TempFolderPath) 
                    ? _settings.TempFolderPath 
                    : Path.GetTempPath();

                var status = new ImportStatus
                {
                    LastImportedProvider = providerName,
                    DateLastImport = now,
                    LastImportStatus = "Started"
                };

                try
                {
                    var stopwatch = Stopwatch.StartNew();

                    var apiKeys = _config.AsEnumerable()
                        .Where(k => k.Key.StartsWith("OCPI-") || k.Key.StartsWith("IMPORT-"))
                        .ToDictionary(k => k.Key, v => v.Value);

                    var importManager = new ImportManager(_settings, apiKeys["IMPORT-ocm-system"], _logger);

                    var importedOK = await importManager.PerformImportProcessing(
                        new ImportProcessSettings
                        {
                            ExportType = Providers.ExportType.JSONAPI,
                            DefaultDataPath = tempPath,
                            FetchLiveData = true,
                            FetchExistingFromAPI = true,
                            PerformDeduplication = true,
                            ProviderName = providerName,
                            Credentials = apiKeys
                        });

                    stopwatch.Stop();

                    if (importedOK)
                    {
                        // Success - remove from failed imports tracking
                        RemoveFailedImport(providerName);

                        status.LastImportStatus = "Completed Successfully";
                        status.DateLastImport = DateTime.UtcNow;
                        status.ProcessingTimeSeconds = stopwatch.Elapsed.TotalSeconds;

                        _logger.LogInformation(
                            $"Import for '{providerName}' completed successfully in {status.ProcessingTimeSeconds:F1} seconds"
                        );
                    }
                    else
                    {
                        // Import completed but with errors - track as failed
                        RecordFailedImport(providerName, "Import completed with errors");

                        status.LastImportStatus = "Completed with errors";
                        status.DateLastImport = DateTime.UtcNow;
                        status.ProcessingTimeSeconds = stopwatch.Elapsed.TotalSeconds;

                        _logger.LogWarning(
                            $"Import for '{providerName}' completed with errors in {status.ProcessingTimeSeconds:F1} seconds. " +
                            $"Will not retry for {IMPORT_DUE_THRESHOLD_HOURS} hours."
                        );
                    }

                    // Save status to file for reference
                    var statusPath = Path.Combine(tempPath, "import_status.json");
                    File.WriteAllText(statusPath, System.Text.Json.JsonSerializer.Serialize(status, 
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
                catch (Exception exp)
                {
                    // Exception occurred - track as failed
                    RecordFailedImport(providerName, exp.Message);

                    status.LastImportStatus = $"Failed: {exp.Message}";
                    status.ProcessingTimeSeconds = 0;
                    
                    _logger.LogError(exp, 
                        $"Import failed for provider '{providerName}': {exp.Message}. " +
                        $"Will not retry for {IMPORT_DUE_THRESHOLD_HOURS} hours."
                    );
                    
                    // Save error status
                    var statusPath = Path.Combine(tempPath, "import_status.json");
                    File.WriteAllText(statusPath, System.Text.Json.JsonSerializer.Serialize(status,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, $"Critical error in import worker: {exp.Message}");
            }
            finally
            {
                _isImportInProgress = false;
            }
        }

        /// <summary>
        /// Check if a provider recently failed and shouldn't be retried yet
        /// </summary>
        private bool IsProviderRecentlyFailed(string providerName, DateTime now)
        {
            lock (_failedImportsLock)
            {
                if (_failedImports.TryGetValue(providerName, out var failedInfo))
                {
                    var timeSinceFailure = now - failedInfo.FailedAt;
                    return timeSinceFailure.TotalHours < IMPORT_DUE_THRESHOLD_HOURS;
                }
                return false;
            }
        }

        /// <summary>
        /// Record a failed import to prevent retry within threshold
        /// </summary>
        private void RecordFailedImport(string providerName, string errorMessage)
        {
            lock (_failedImportsLock)
            {
                if (_failedImports.ContainsKey(providerName))
                {
                    var existingInfo = _failedImports[providerName];
                    existingInfo.FailureCount++;
                    existingInfo.LastErrorMessage = errorMessage;
                    existingInfo.FailedAt = DateTime.UtcNow;
                    
                    _logger.LogWarning(
                        $"Provider '{providerName}' has failed {existingInfo.FailureCount} time(s). " +
                        $"Latest error: {errorMessage}"
                    );
                }
                else
                {
                    _failedImports[providerName] = new FailedImportInfo
                    {
                        ProviderName = providerName,
                        FailedAt = DateTime.UtcNow,
                        LastErrorMessage = errorMessage,
                        FailureCount = 1
                    };
                    
                    _logger.LogWarning($"Provider '{providerName}' added to failed imports tracking");
                }
            }
        }

        /// <summary>
        /// Remove a provider from failed imports tracking after successful import
        /// </summary>
        private void RemoveFailedImport(string providerName)
        {
            lock (_failedImportsLock)
            {
                if (_failedImports.Remove(providerName))
                {
                    _logger.LogInformation($"Provider '{providerName}' removed from failed imports tracking after successful import");
                }
            }
        }

        /// <summary>
        /// Clean up failed imports older than the threshold
        /// </summary>
        private void CleanupFailedImports()
        {
            lock (_failedImportsLock)
            {
                var now = DateTime.UtcNow;
                var toRemove = _failedImports
                    .Where(kvp => (now - kvp.Value.FailedAt).TotalHours >= IMPORT_DUE_THRESHOLD_HOURS)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var providerName in toRemove)
                {
                    _failedImports.Remove(providerName);
                    _logger.LogInformation(
                        $"Provider '{providerName}' removed from failed imports tracking - threshold expired, will be retried if due"
                    );
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Import Worker is stopping");
            
            // Log current failed imports state
            lock (_failedImportsLock)
            {
                if (_failedImports.Any())
                {
                    _logger.LogInformation($"Current failed imports being tracked: {string.Join(", ", _failedImports.Keys)}");
                }
            }

            _timer?.Change(Timeout.Infinite, 0);
            _timer?.Dispose();

            await base.StopAsync(stoppingToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }

    /// <summary>
    /// Tracks information about failed imports
    /// </summary>
    internal class FailedImportInfo
    {
        public string ProviderName { get; set; }
        public DateTime FailedAt { get; set; }
        public string LastErrorMessage { get; set; }
        public int FailureCount { get; set; }
    }
}
