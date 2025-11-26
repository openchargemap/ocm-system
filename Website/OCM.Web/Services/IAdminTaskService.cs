using System.Collections.Generic;
using System.Threading.Tasks;
using OCM.Core.Data;

namespace OCM.Web.Services
{
    public interface IAdminTaskService
    {
        Task<AdminTaskResult> ExecutePeriodicTasksAsync();
    }

    public class AdminTaskResult
    {
        public int NotificationsSent { get; set; }
        public int ItemsAutoApproved { get; set; }
        public int ExceptionCount { get; set; }
        public List<string> LogItems { get; set; } = new List<string>();
        public MirrorStatus MirrorStatus { get; set; }
    }
}
