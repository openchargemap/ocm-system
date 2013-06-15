using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OCM.API.Common.Model;
using System.Collections;
using System.Configuration;

namespace OCM.API.Common
{
    public enum AuditEventType
    {
        PermissionRequested = 10,
        PermissionGranted = 20,
        PermissionRemoved = 30,
        CreatedItem = 100,
        UpdatedItem = 200,
        DeletedItem = 300,
        SystemErrorWeb = 1000,
        SystemErrorAPI = 1010
    }

    public class AuditLogManager
    {
        public static void ReportWebException(HttpServerUtility Server, AuditEventType eventType)
        {

            bool ignoreException = false;
            string body = "An error has occurred while a user was browsing OCM:<br><br>";

            object exceptionObject = null;
            if (Server.GetLastError() != null)
            {
                Exception exp = Server.GetLastError();
                exceptionObject = exp;

                if (exp.InnerException != null)
                {
                    exceptionObject = exp.InnerException;
                }

                body += ((Exception)exceptionObject).ToString();

                HttpContext con = HttpContext.Current;
                if (con.Request.Url != null)
                {
                    body += "<br><br>Request Url:" + con.Request.Url.ToString();

                    //special case to avoid reporting /trackback url exceptions
                    if (con.Request.Url.ToString().EndsWith("/trackback/")) ignoreException = true;

                }
                body += "<br><br>" + DateTime.UtcNow.ToString();
            }

            if (exceptionObject is System.Web.HttpRequestValidationException || exceptionObject is System.Web.UI.ViewStateException) ignoreException = true;

            if (!ignoreException)
            {
                if (ConfigurationManager.AppSettings["EnableErrorNotifications"] == "true")
                {
                    NotificationManager notification = new NotificationManager();

                    var msgParams = new Hashtable(){
                    {"Description", "System Error"},
                    {"Name", "OCM Website"},
                    {"Email", "system@openchargemap.org"},
                    {"Comment", body}
                };

                    notification.PrepareNotification(NotificationType.ContactUsMessage, msgParams);
                    notification.SendNotification(NotificationType.ContactUsMessage);

                }

                AuditLogManager.Log(null, eventType, body, null);

            }
        }

        public static void Log(User user, AuditEventType eventType, string eventDescription, string comment)
        {
            try
            {
                OCM.Core.Data.OCMEntities dataModel = new OCM.Core.Data.OCMEntities();
                var auditEntry = new Core.Data.AuditLog();

                if (user == null)
                {
                    auditEntry.User = dataModel.Users.First(u => u.ID == (int)StandardUsers.System);
                }
                else
                {
                    auditEntry.UserID = user.ID;
                }

                auditEntry.EventDescription = "[" + eventType.ToString() + "]:" + (eventDescription != null ? eventDescription : "");
                auditEntry.Comment = comment;
                auditEntry.EventDate = DateTime.UtcNow;

                dataModel.AuditLogs.Add(auditEntry);
                dataModel.SaveChanges();

                System.Diagnostics.Debug.WriteLine("Log:"+auditEntry.EventDescription);
            }
            catch (Exception)
            {
                //TODO: fallback to alternative logging
            }
        }

        public List<AuditLog> Get(int? userID, DateTime? fromDate)
        {
            OCM.Core.Data.OCMEntities dataModel = new Core.Data.OCMEntities();
            var list = dataModel.AuditLogs.Where(a =>
                    (fromDate == null || (fromDate != null && a.EventDate >= fromDate))
                    && (userID == null || (userID != null && a.UserID == userID))
                ).OrderByDescending(a => a.EventDate);

            List<AuditLog> auditLog = new List<AuditLog>();
            foreach (var log in list)
            {
                auditLog.Add(Model.Extensions.AuditLog.FromDataModel(log));
            }
            return auditLog;
        }
    }
}