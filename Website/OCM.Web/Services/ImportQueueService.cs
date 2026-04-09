using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCM.API.Common;
using OCM.Import;
using OCM.Import.Providers;
using OCM.Import.Providers.OCPI;
using OCM.Web.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace OCM.Web.Services
{
    public class ImportQueueService : IImportQueueService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ImportQueueService> _logger;
        private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();
        private readonly ConcurrentDictionary<Guid, ImportJobState> _jobs = new ConcurrentDictionary<Guid, ImportJobState>();
        private readonly ConcurrentDictionary<int, Guid> _activeAgreementJobs = new ConcurrentDictionary<int, Guid>();

        public ImportQueueService(IConfiguration configuration, ILogger<ImportQueueService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public ImportJobViewModel QueueImport(int agreementId, int requestedByUserId, ImportJobMode mode = ImportJobMode.Import)
        {
            if (_activeAgreementJobs.TryGetValue(agreementId, out var existingJobId)
                && _jobs.TryGetValue(existingJobId, out var existingJob)
                && (existingJob.Status == ImportJobStatus.Queued || existingJob.Status == ImportJobStatus.Running))
            {
                return CreateSnapshot(existingJob);
            }

            var job = new ImportJobState
            {
                JobId = Guid.NewGuid(),
                AgreementId = agreementId,
                RequestedByUserId = requestedByUserId,
                QueuedUtc = DateTime.UtcNow,
                Mode = mode,
                Status = ImportJobStatus.Queued,
                StatusMessage = mode == ImportJobMode.Preview ? "Import preview queued." : "Import queued."
            };

            _jobs[job.JobId] = job;
            _activeAgreementJobs[agreementId] = job.JobId;

            AddLog(job, LogLevel.Information, $"Queued {mode.ToString().ToLowerInvariant()} job for agreement {agreementId}.");
            PublishStatus(job);
            _queue.Writer.TryWrite(job.JobId);

            return CreateSnapshot(job);
        }

        public IReadOnlyCollection<ImportJobViewModel> QueueApprovedImports(int requestedByUserId, ImportJobMode mode = ImportJobMode.Import)
        {
            using var dataProviderManager = new DataProviderManager();

            var agreementIds = dataProviderManager.GetApprovedImportAgreementIds();
            var jobs = new List<ImportJobViewModel>();

            foreach (var agreementId in agreementIds)
            {
                jobs.Add(QueueImport(agreementId, requestedByUserId, mode));
            }

            return jobs;
        }

        public ImportJobViewModel GetJob(Guid jobId)
        {
            return _jobs.TryGetValue(jobId, out var job) ? CreateSnapshot(job) : null;
        }

        public ImportJobViewModel GetLatestJobForAgreement(int agreementId)
        {
            var job = _jobs.Values
                .Where(j => j.AgreementId == agreementId)
                .OrderByDescending(j => j.QueuedUtc)
                .FirstOrDefault();

            return job == null ? null : CreateSnapshot(job);
        }

        public async IAsyncEnumerable<ImportStreamMessage> StreamJobEventsAsync(Guid jobId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!_jobs.TryGetValue(jobId, out var job))
            {
                yield break;
            }

            List<ImportLogEntry> logs;
            ImportStreamMessage statusMessage;
            Channel<ImportStreamMessage> subscriber = null;

            lock (job.SyncRoot)
            {
                logs = job.Logs
                    .Select(log => new ImportLogEntry
                    {
                        Sequence = log.Sequence,
                        TimestampUtc = log.TimestampUtc,
                        Level = log.Level,
                        Message = log.Message
                    })
                    .ToList();

                statusMessage = CreateStatusMessage(job);

                if (job.Status == ImportJobStatus.Queued || job.Status == ImportJobStatus.Running)
                {
                    subscriber = Channel.CreateUnbounded<ImportStreamMessage>();
                    job.Subscribers.Add(subscriber);
                }
            }

            foreach (var log in logs)
            {
                yield return new ImportStreamMessage { EventType = "log", LogEntry = log };
            }

            yield return statusMessage;

            if (subscriber == null)
            {
                yield break;
            }

            try
            {
                await foreach (var message in subscriber.Reader.ReadAllAsync(cancellationToken))
                {
                    yield return message;
                }
            }
            finally
            {
                lock (job.SyncRoot)
                {
                    job.Subscribers.Remove(subscriber);
                }
            }
        }

        public async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            await foreach (var jobId in _queue.Reader.ReadAllAsync(cancellationToken))
            {
                if (_jobs.TryGetValue(jobId, out var job))
                {
                    await ExecuteJobAsync(job, cancellationToken);
                }
            }
        }

        private async Task ExecuteJobAsync(ImportJobState job, CancellationToken cancellationToken)
        {
            var jobLogger = new ImportLogger(this, job.JobId, _logger);

            try
            {
                job.StartedUtc = DateTime.UtcNow;
                job.Status = ImportJobStatus.Running;
                job.StatusMessage = job.Mode == ImportJobMode.Preview ? "Import preview is running." : "Import is running.";
                PublishStatus(job);
                AddLog(job, LogLevel.Information, job.Mode == ImportJobMode.Preview ? "Preparing configured import preview." : "Preparing configured import.");

                using var agreementManager = new DataSharingAgreementManager();
                using var dataProviderManager = new DataProviderManager();

                var agreement = agreementManager.GetAgreement(job.AgreementId);
                if (agreement == null)
                {
                    throw new InvalidOperationException($"Agreement {job.AgreementId} was not found.");
                }

                var dataProvider = dataProviderManager.GetDataProviderByAgreementId(job.AgreementId);
                var importConfig = dataProviderManager.GetImportConfigByAgreementId(job.AgreementId);

                if (dataProvider != null)
                {
                    UpdateProviderDetails(job, dataProvider.ID, dataProvider.Title);
                }
                else if (!string.IsNullOrWhiteSpace(agreement.CompanyName))
                {
                    UpdateProviderDetails(job, null, agreement.CompanyName);
                }

                if (string.IsNullOrWhiteSpace(importConfig))
                {
                    throw new InvalidOperationException("No stored OCPI import configuration was found for this agreement.");
                }

                var providerLoader = new OCPIProviderLoader(jobLogger);
                if (!providerLoader.LoadFromJson(importConfig))
                {
                    throw new InvalidOperationException("Stored OCPI import configuration could not be parsed.");
                }

                var importProvider = providerLoader.CreateProviders(enabledOnly: false).FirstOrDefault();
                if (importProvider == null)
                {
                    throw new InvalidOperationException("No import provider could be created from the stored configuration.");
                }

                UpdateProviderDetails(job, dataProvider?.ID ?? importProvider.GetProviderID(), importProvider.GetProviderName());
                AddLog(job, LogLevel.Information, $"Starting {job.Mode.ToString().ToLowerInvariant()} for provider '{importProvider.GetProviderName()}'.");

                var importSettings = new ImportSettings();
                _configuration.GetSection("ImportSettings").Bind(importSettings);
                if (string.IsNullOrWhiteSpace(importSettings.TempFolderPath))
                {
                    importSettings.TempFolderPath = Path.GetTempPath();
                }

                if (string.IsNullOrWhiteSpace(importSettings.ImportUserAgent))
                {
                    importSettings.ImportUserAgent = "OCM.Web-AgreementImport";
                }

                var credentials = _configuration.AsEnumerable()
                    .Where(k => !string.IsNullOrWhiteSpace(k.Value))
                    .Where(k => k.Key.StartsWith("OCPI-", StringComparison.OrdinalIgnoreCase) || k.Key.StartsWith("IMPORT-", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(k => k.Key, k => k.Value, StringComparer.OrdinalIgnoreCase);

                if (!credentials.TryGetValue("IMPORT-ocm-system", out var systemApiKey) || string.IsNullOrWhiteSpace(systemApiKey))
                {
                    throw new InvalidOperationException("The IMPORT-ocm-system credential is not configured for web imports.");
                }

                var importManager = new ImportManager(importSettings, systemApiKey, jobLogger);
                var importReport = await importManager.PerformImportProcessingWithReport(
                    new ImportProcessSettings
                    {
                        ExportType = ExportType.JSONAPI,
                        DefaultDataPath = importSettings.TempFolderPath,
                        FetchLiveData = true,
                        FetchExistingFromAPI = true,
                        PerformDeduplication = true,
                        ProviderName = importProvider.GetProviderName(),
                        IsPreviewMode = job.Mode == ImportJobMode.Preview,
                        PreviewItemLimit = 100,
                        Credentials = credentials
                    },
                    importProvider);

                if (job.Mode == ImportJobMode.Preview)
                {
                    job.PreviewSummary = CreatePreviewSummary(importReport);
                    AddPreviewSummaryLog(job, job.PreviewSummary);
                }

                if (importReport?.IsSuccess != true)
                {
                    throw new InvalidOperationException(job.Mode == ImportJobMode.Preview
                        ? "Import preview completed with errors. Review the log output for details."
                        : "Import completed with errors. Review the log output for details.");
                }

                if (job.Mode == ImportJobMode.Import && job.DataProviderId.HasValue)
                {
                    dataProviderManager.UpdateDateLastImport(job.DataProviderId.Value);
                    AddLog(job, LogLevel.Information, $"Updated last imported timestamp for data provider {job.DataProviderId.Value}.");
                }

                job.Status = ImportJobStatus.Completed;
                job.StatusMessage = job.Mode == ImportJobMode.Preview ? "Import preview completed successfully." : "Import completed successfully.";
                job.CompletedUtc = DateTime.UtcNow;
                PublishStatus(job);
                AddLog(job, LogLevel.Information, job.Mode == ImportJobMode.Preview ? "Import preview completed successfully." : "Import completed successfully.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                job.Status = ImportJobStatus.Failed;
                job.StatusMessage = "Import cancelled because the application is stopping.";
                job.LastError = job.StatusMessage;
                job.CompletedUtc = DateTime.UtcNow;
                PublishStatus(job);
                AddLog(job, LogLevel.Warning, job.StatusMessage);
            }
            catch (Exception ex)
            {
                job.LastError = ex.Message;
                job.Status = ImportJobStatus.Failed;
                job.StatusMessage = job.Mode == ImportJobMode.Preview ? "Import preview failed." : "Import failed.";
                job.CompletedUtc = DateTime.UtcNow;
                jobLogger.LogError(ex, "Import job {JobId} failed", job.JobId);
                PublishStatus(job);
            }
            finally
            {
                _activeAgreementJobs.TryRemove(job.AgreementId, out _);
                CompleteSubscribers(job);
            }
        }

        private void UpdateProviderDetails(ImportJobState job, int? dataProviderId, string providerName)
        {
            lock (job.SyncRoot)
            {
                job.DataProviderId = dataProviderId;
                if (!string.IsNullOrWhiteSpace(providerName))
                {
                    job.ProviderName = providerName;
                }
            }
        }

        private void AddLog(Guid jobId, LogLevel level, string message)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                AddLog(job, level, message);
            }
        }

        private void AddLog(ImportJobState job, LogLevel level, string message)
        {
            ImportLogEntry entry;

            lock (job.SyncRoot)
            {
                entry = new ImportLogEntry
                {
                    Sequence = ++job.LogSequence,
                    TimestampUtc = DateTime.UtcNow,
                    Level = level.ToString(),
                    Message = message
                };

                job.Logs.Add(entry);
            }

            Broadcast(job, new ImportStreamMessage
            {
                EventType = "log",
                LogEntry = entry
            });
        }

        private void AddPreviewSummaryLog(ImportJobState job, ImportPreviewSummaryViewModel summary)
        {
            if (summary == null)
            {
                return;
            }

            var message = new StringBuilder()
                .Append("Preview summary: source=")
                .Append(summary.SourceItemCount)
                .Append(", processed=")
                .Append(summary.PreviewItemCount)
                .Append(", added=")
                .Append(summary.AddedCount)
                .Append(", updated=")
                .Append(summary.UpdatedCount)
                .Append(", unchanged=")
                .Append(summary.UnchangedCount)
                .Append(", duplicates=")
                .Append(summary.DuplicateCount)
                .Append(", low-quality=")
                .Append(summary.LowDataQualityCount)
                .Append(", delisted=")
                .Append(summary.DelistedCount)
                .ToString();

            AddLog(job, LogLevel.Information, message);
        }

        private void PublishStatus(ImportJobState job)
        {
            Broadcast(job, CreateStatusMessage(job));
        }

        private ImportStreamMessage CreateStatusMessage(ImportJobState job)
        {
            lock (job.SyncRoot)
            {
                return new ImportStreamMessage
                {
                    EventType = "status",
                    Mode = job.Mode.ToString(),
                    Status = job.Status.ToString(),
                    StatusMessage = job.StatusMessage,
                    LastError = job.LastError,
                    PreviewSummary = ClonePreviewSummary(job.PreviewSummary)
                };
            }
        }

        private void Broadcast(ImportJobState job, ImportStreamMessage message)
        {
            List<Channel<ImportStreamMessage>> subscribers;

            lock (job.SyncRoot)
            {
                subscribers = job.Subscribers.ToList();
            }

            foreach (var subscriber in subscribers)
            {
                subscriber.Writer.TryWrite(message);
            }
        }

        private void CompleteSubscribers(ImportJobState job)
        {
            List<Channel<ImportStreamMessage>> subscribers;

            lock (job.SyncRoot)
            {
                subscribers = job.Subscribers.ToList();
                job.Subscribers.Clear();
            }

            foreach (var subscriber in subscribers)
            {
                subscriber.Writer.TryComplete();
            }
        }

        private static ImportJobViewModel CreateSnapshot(ImportJobState job)
        {
            lock (job.SyncRoot)
            {
                return new ImportJobViewModel
                {
                    JobId = job.JobId,
                    AgreementId = job.AgreementId,
                    DataProviderId = job.DataProviderId,
                    ProviderName = job.ProviderName,
                    Mode = job.Mode,
                    Status = job.Status,
                    StatusMessage = job.StatusMessage,
                    QueuedUtc = job.QueuedUtc,
                    StartedUtc = job.StartedUtc,
                    CompletedUtc = job.CompletedUtc,
                    RequestedByUserId = job.RequestedByUserId,
                    LastError = job.LastError,
                    PreviewSummary = ClonePreviewSummary(job.PreviewSummary),
                    Logs = job.Logs
                        .Select(log => new ImportLogEntry
                        {
                            Sequence = log.Sequence,
                            TimestampUtc = log.TimestampUtc,
                            Level = log.Level,
                            Message = log.Message
                        })
                        .ToList()
                };
            }
        }

        private sealed class ImportJobState
        {
            public Guid JobId { get; set; }
            public int AgreementId { get; set; }
            public int? DataProviderId { get; set; }
            public string ProviderName { get; set; }
            public ImportJobMode Mode { get; set; }
            public ImportJobStatus Status { get; set; }
            public string StatusMessage { get; set; }
            public DateTime QueuedUtc { get; set; }
            public DateTime? StartedUtc { get; set; }
            public DateTime? CompletedUtc { get; set; }
            public int RequestedByUserId { get; set; }
            public string LastError { get; set; }
            public ImportPreviewSummaryViewModel PreviewSummary { get; set; }
            public List<ImportLogEntry> Logs { get; set; } = new List<ImportLogEntry>();
            public long LogSequence { get; set; }
            public List<Channel<ImportStreamMessage>> Subscribers { get; } = new List<Channel<ImportStreamMessage>>();
            public object SyncRoot { get; } = new object();
        }

        private static ImportPreviewSummaryViewModel CreatePreviewSummary(ImportReport report)
        {
            if (report == null)
            {
                return null;
            }

            return new ImportPreviewSummaryViewModel
            {
                SourceItemCount = report.SourceItemCount,
                PreviewItemCount = report.ProcessedSourceItemCount,
                PreviewItemLimit = report.PreviewItemLimit,
                AddedCount = report.Added?.Count ?? 0,
                UpdatedCount = report.Updated?.Count ?? 0,
                UnchangedCount = report.Unchanged?.Count ?? 0,
                DuplicateCount = report.Duplicates?.Count ?? 0,
                LowDataQualityCount = report.LowDataQuality?.Count ?? 0,
                DelistedCount = report.Delisted?.Count ?? 0,
                AddedItems = CreatePreviewItems(report.Added),
                UpdatedItems = CreatePreviewItems(report.Updated),
                DelistedItems = CreatePreviewItems(report.Delisted),
                LowDataQualityItems = CreatePreviewItems(report.LowDataQuality)
            };
        }

        private static List<ImportPreviewItemViewModel> CreatePreviewItems(IEnumerable<OCM.API.Common.Model.ChargePoint> items)
        {
            return (items ?? Enumerable.Empty<OCM.API.Common.Model.ChargePoint>())
                .Take(10)
                .Select(item => new ImportPreviewItemViewModel
                {
                    OcmId = item?.ID > 0 ? item.ID : null,
                    Title = item?.AddressInfo?.Title,
                    Address = string.Join(", ", new[]
                    {
                        item?.AddressInfo?.AddressLine1,
                        item?.AddressInfo?.Town,
                        item?.AddressInfo?.StateOrProvince
                    }.Where(v => !string.IsNullOrWhiteSpace(v))),
                    Reference = item?.DataProvidersReference
                })
                .ToList();
        }

        private static ImportPreviewSummaryViewModel ClonePreviewSummary(ImportPreviewSummaryViewModel summary)
        {
            if (summary == null)
            {
                return null;
            }

            return new ImportPreviewSummaryViewModel
            {
                SourceItemCount = summary.SourceItemCount,
                PreviewItemCount = summary.PreviewItemCount,
                PreviewItemLimit = summary.PreviewItemLimit,
                AddedCount = summary.AddedCount,
                UpdatedCount = summary.UpdatedCount,
                UnchangedCount = summary.UnchangedCount,
                DuplicateCount = summary.DuplicateCount,
                LowDataQualityCount = summary.LowDataQualityCount,
                DelistedCount = summary.DelistedCount,
                AddedItems = summary.AddedItems?.Select(ClonePreviewItem).ToList() ?? new List<ImportPreviewItemViewModel>(),
                UpdatedItems = summary.UpdatedItems?.Select(ClonePreviewItem).ToList() ?? new List<ImportPreviewItemViewModel>(),
                DelistedItems = summary.DelistedItems?.Select(ClonePreviewItem).ToList() ?? new List<ImportPreviewItemViewModel>(),
                LowDataQualityItems = summary.LowDataQualityItems?.Select(ClonePreviewItem).ToList() ?? new List<ImportPreviewItemViewModel>()
            };
        }

        private static ImportPreviewItemViewModel ClonePreviewItem(ImportPreviewItemViewModel item)
        {
            if (item == null)
            {
                return null;
            }

            return new ImportPreviewItemViewModel
            {
                OcmId = item.OcmId,
                Title = item.Title,
                Address = item.Address,
                Reference = item.Reference
            };
        }

        private sealed class ImportLogger : ILogger
        {
            private readonly ImportQueueService _queueService;
            private readonly Guid _jobId;
            private readonly ILogger _fallbackLogger;

            public ImportLogger(ImportQueueService queueService, Guid jobId, ILogger fallbackLogger)
            {
                _queueService = queueService;
                _jobId = jobId;
                _fallbackLogger = fallbackLogger;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var message = formatter != null ? formatter(state, exception) : state?.ToString();
                if (exception != null)
                {
                    message = string.IsNullOrWhiteSpace(message) ? exception.ToString() : $"{message}{Environment.NewLine}{exception}";
                }

                if (!string.IsNullOrWhiteSpace(message))
                {
                    _queueService.AddLog(_jobId, logLevel, message);
                }

                _fallbackLogger?.Log(logLevel, eventId, state, exception, formatter);
            }

            private sealed class NullScope : IDisposable
            {
                public static NullScope Instance { get; } = new NullScope();

                public void Dispose()
                {
                }
            }
        }
    }
}
