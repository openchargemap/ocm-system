using System;

namespace OCM.Core.Data
{
    public partial class MediaItem
    {
        public int Id { get; set; }
        public string ItemUrl { get; set; }
        public string ItemThumbnailUrl { get; set; }
        public string Comment { get; set; }
        public int ChargePointId { get; set; }
        public int UserId { get; set; }
        public DateTime DateCreated { get; set; }
        public bool? IsEnabled { get; set; }
        public bool IsVideo { get; set; }
        public bool IsFeaturedItem { get; set; }
        public string MetadataValue { get; set; }
        public bool IsExternalResource { get; set; }

        public virtual ChargePoint ChargePoint { get; set; }
        public virtual User User { get; set; }
    }
}
