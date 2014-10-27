using OCM.API.Common;
using OCM.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common
{
    public class UserCommentManager: ManagerBase
    {
        public List<OCM.API.Common.Model.UserComment> GetUserComments(int userId)
        {
            var list = DataModel.UserComments.Where(u => u.UserID == userId);

            var results = new List<OCM.API.Common.Model.UserComment>();
            foreach(var c in list)
            {
                results.Add(OCM.API.Common.Model.Extensions.UserComment.FromDataModel(c, true));
            }

            return results;
        }

        public void DeleteComment(int userId, int commentId)
        {
            var comment = DataModel.UserComments.FirstOrDefault(c=>c.ID==commentId);
            
            if (comment!=null){
                var cpID = comment.ChargePointID;
                DataModel.UserComments.Remove(comment);
                DataModel.ChargePoints.Find(cpID).DateLastStatusUpdate = DateTime.UtcNow;
                DataModel.SaveChanges();

                var user = new UserManager().GetUser(userId);
                AuditLogManager.Log(user, AuditEventType.DeletedItem, "{EntityType:\"Comment\",EntityID:" + commentId + ",ChargePointID:" + cpID + "}", "User deleted comment");

                CacheManager.RefreshCachedPOIList();
                
            }
            
        }
    }
}
