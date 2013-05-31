using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OCM.API.Common.Model;

namespace OCM.API.Common
{
    public enum AuditEventType
    {
        PermissionRequested = 10,
        PermissionGranted = 20,
        PermissionRemoved = 30,
        CreatedItem = 100,
        UpdatedItem = 200,
        DeletedItem = 300
    }

    public class AuditLogManager
    {
        
        public static void Log(User user, AuditEventType eventType, string eventDescription, string comment)
        {
            OCM.Core.Data.OCMEntities dataModel = new OCM.Core.Data.OCMEntities();
            var auditEntry = new Core.Data.AuditLog();
            auditEntry.UserID = user.ID;
            auditEntry.EventDescription = "["+eventType.ToString()+"]:" +(eventDescription!=null?eventDescription:"");
            auditEntry.Comment = comment;
            auditEntry.EventDate = DateTime.UtcNow;
            
            dataModel.AuditLogs.Add(auditEntry);
            dataModel.SaveChanges();
        }

        public List<AuditLog> Get(int? userID, DateTime? fromDate)
        {
            OCM.Core.Data.OCMEntities dataModel = new Core.Data.OCMEntities();
            var list = dataModel.AuditLogs.Where(a=>
                    (fromDate==null || (fromDate!=null && a.EventDate>=fromDate))
                    && (userID==null || (userID!=null && a.UserID==userID))
                ).OrderByDescending(a=>a.EventDate);

            List<AuditLog> auditLog = new List<AuditLog>();
            foreach (var log in list)
            {
                auditLog.Add(Model.Extensions.AuditLog.FromDataModel(log));
            }
            return auditLog;
        }
    }
}