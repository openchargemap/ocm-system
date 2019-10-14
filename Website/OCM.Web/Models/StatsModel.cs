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
        public GeneralStats TotalActiveEditors { get; set; } //ActiveEditorsLast90Days
        public GeneralStats TotalActiveContributors { get; set; }//TotalChangeContributorsLast90Days
        public GeneralStats TotalCommentContributors { get; set; }//TotalCommentContributorsLast90Days
        public GeneralStats TotalPhotoContributors { get; set; }//TotalPhotoContributorsLast90Days
    }
}