using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OCM.Web.Models;

namespace OCM.Web.Services
{
    public interface IImportQueueService
    {
        ImportJobViewModel QueueImport(int agreementId, int requestedByUserId, ImportJobMode mode = ImportJobMode.Import);
        IReadOnlyCollection<ImportJobViewModel> QueueApprovedImports(int requestedByUserId, ImportJobMode mode = ImportJobMode.Import);
        ImportJobViewModel GetJob(Guid jobId);
        ImportJobViewModel GetLatestJobForAgreement(int agreementId);
        IAsyncEnumerable<ImportStreamMessage> StreamJobEventsAsync(Guid jobId, CancellationToken cancellationToken);
        Task ProcessQueueAsync(CancellationToken cancellationToken);
    }

    public class ImportStreamMessage
    {
        public string EventType { get; set; }
        public ImportLogEntry LogEntry { get; set; }
        public string Status { get; set; }
        public string StatusMessage { get; set; }
        public string LastError { get; set; }
        public string Mode { get; set; }
        public ImportPreviewSummaryViewModel PreviewSummary { get; set; }
    }
}
