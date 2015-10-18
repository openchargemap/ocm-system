using OCM.API.Common.DataSummary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCM.MVC.Models
{
    public class StatsModel
    {
        public List<GeneralStats> TopContributors { get; set; }
        public List<GeneralStats> TopCommentators { get; set; }
        public List<GeneralStats> TopMediaItems { get; set; }
        public List<GeneralStats> UserRegistrations { get; set; }
        public List<GeneralStats> UserComments { get; set; }
        public List<UserEditStats> UserEdits { get; set; }
    }
}