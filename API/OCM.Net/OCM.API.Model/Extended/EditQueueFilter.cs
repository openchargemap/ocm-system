using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCM.API.Common.Model
{
    public class EditQueueFilter
    {
        public bool ShowEditsOnly { get; set; }
        public bool ShowProcessed { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int MinimumDifferences { get; set; }
        public int MaxResults { get; set; }

        public EditQueueFilter()
        {
            MinimumDifferences = 1;
            MaxResults = 200;
        }
    }
}