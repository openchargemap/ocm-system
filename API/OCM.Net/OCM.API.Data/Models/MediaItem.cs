using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class MediaItem
    {
        public int ID { get; set; }
        public string ItemURL { get; set; }
        public string ItemThumbnailURL { get; set; }
        public string Comment { get; set; }
        public int ChargePointID { get; set; }
        public int UserID { get; set; }
        public System.DateTime DateCreated { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsVideo { get; set; }
        public bool IsFeaturedItem { get; set; }
        public string MetadataValue { get; set; }
        public bool IsExternalResource { get; set; }
        public virtual ChargePoint ChargePoint { get; set; }
        public virtual User User { get; set; }
    }
}
