using System.Collections.Generic;

namespace OCM.API.Common.Model
{
    public class ApplicationSummary
    {

        public List<RegisteredApplication> RegisteredApplications { get; set; }
        public List<RegisteredApplicationUser> AuthorizedApplications { get; set; }
    }
}
