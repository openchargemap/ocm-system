using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common.Model
{
    public class MediaItem
    {
        public int ID { get; set; }
        public int ChargePointID { get; set; }
        public string ItemURL { get; set; }
        public string ItemThumbnailURL { get; set; }
        public string Comment { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsVideo { get; set; }
        public bool IsFeaturedItem { get; set; }
        public bool IsExternalResource { get; set; }
        public string MetadataValue { get; set; }
        public User User { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
