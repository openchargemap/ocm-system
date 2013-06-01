using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common.Model
{
    public class POIRecentActivity
    {
        public List<ChargePoint> POIRecentlyAdded { get; set; }
        public List<ChargePoint> POIRecentlyUpdated { get; set; }
        public List<UserComment> RecentComments { get; set; }
        public List<MediaItem> RecentMedia { get; set; }
        public List<User> MostActiveUsers { get; set; }
    }
}
