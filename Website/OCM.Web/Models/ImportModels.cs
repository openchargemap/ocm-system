using System;
using System.Collections.Generic;

namespace OCM.Web.Models
{
    public enum ImportJobStatus
    {
        Queued,
        Running,
        Completed,
        Failed
    }

    public enum ImportJobMode
    {
        Import,
        Preview
    }

    public class ImportLogEntry
    {
        public long Sequence { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
    }

    public class ImportPreviewItemViewModel
    {
        public int? OcmId { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public string Reference { get; set; }
    }

    public class ImportPreviewSummaryViewModel
    {
        public int SourceItemCount { get; set; }
        public int PreviewItemCount { get; set; }
        public int PreviewItemLimit { get; set; }
        public int AddedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int UnchangedCount { get; set; }
        public int DuplicateCount { get; set; }
        public int LowDataQualityCount { get; set; }
        public int DelistedCount { get; set; }
        public List<ImportPreviewItemViewModel> AddedItems { get; set; } = new List<ImportPreviewItemViewModel>();
        public List<ImportPreviewItemViewModel> UpdatedItems { get; set; } = new List<ImportPreviewItemViewModel>();
        public List<ImportPreviewItemViewModel> DelistedItems { get; set; } = new List<ImportPreviewItemViewModel>();
        public List<ImportPreviewItemViewModel> LowDataQualityItems { get; set; } = new List<ImportPreviewItemViewModel>();
    }

    public class ImportJobViewModel
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
        public List<ImportLogEntry> Logs { get; set; } = new List<ImportLogEntry>();
        public ImportPreviewSummaryViewModel PreviewSummary { get; set; }

        public bool IsActive => Status == ImportJobStatus.Queued || Status == ImportJobStatus.Running;
        public string ModeText => Mode.ToString();
        public string StatusText => Status.ToString();
    }
}
