using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class ChargePoint
    {
        public ChargePoint()
        {
            ConnectionInfos = new HashSet<ConnectionInfo>();
            InverseParentChargePoint = new HashSet<ChargePoint>();
            MediaItems = new HashSet<MediaItem>();
            MetadataValues = new HashSet<MetadataValue>();
            UserComments = new HashSet<UserComment>();
        }

        public int Id { get; set; }
        public string Uuid { get; set; }
        public int? ParentChargePointId { get; set; }
        public int DataProviderId { get; set; }
        public string DataProvidersReference { get; set; }
        public int? OperatorId { get; set; }
        public string OperatorsReference { get; set; }
        public int? UsageTypeId { get; set; }
        public int? AddressInfoId { get; set; }
        public int? NumberOfPoints { get; set; }
        public string GeneralComments { get; set; }
        public DateTime? DatePlanned { get; set; }
        public DateTime? DateLastConfirmed { get; set; }
        public int? StatusTypeId { get; set; }
        public DateTime? DateLastStatusUpdate { get; set; }
        public int? DataQualityLevel { get; set; }
        public DateTime? DateCreated { get; set; }
        public int? SubmissionStatusTypeId { get; set; }
        public string UsageCost { get; set; }
        public short? LevelOfDetail { get; set; }

        public virtual AddressInfo AddressInfo { get; set; }
        public virtual DataProvider DataProvider { get; set; }
        public virtual Operator Operator { get; set; }
        public virtual ChargePoint ParentChargePoint { get; set; }
        public virtual StatusType StatusType { get; set; }
        public virtual SubmissionStatusType SubmissionStatusType { get; set; }
        public virtual UsageType UsageType { get; set; }
        public virtual ICollection<ConnectionInfo> ConnectionInfos { get; set; }
        public virtual ICollection<ChargePoint> InverseParentChargePoint { get; set; }
        public virtual ICollection<MediaItem> MediaItems { get; set; }
        public virtual ICollection<MetadataValue> MetadataValues { get; set; }
        public virtual ICollection<UserComment> UserComments { get; set; }
    }
}
