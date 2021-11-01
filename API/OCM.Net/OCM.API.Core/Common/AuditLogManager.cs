using OCM.API.Common.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
        SystemErrorAPI = 1010,
        StatisticsUpdated = 2000
    }

    public class AuditLogManager
    {
        public static void ReportWebException(bool enableNotifications, string contextUrl, AuditEventType eventType, string msg = null, Exception exp = null)
        {
            bool ignoreException = false;
            string body = "An error has occurred while a user was browsing OCM:<br><br>";

            if (msg != null)
            {
                body = msg;
            }


            if (exp != null)
            {

                object exceptionObject = exp;

                if (exp.InnerException != null)
                {
                    exceptionObject = exp.InnerException;
                }

                body += ((Exception)exceptionObject).ToString();


                if (contextUrl != null)
                {
                    body += "<br><br>Request Url:" + contextUrl.ToString();

                    //special case to avoid reporting /trackback url exceptions
                    if (contextUrl.ToString().EndsWith("/trackback/")) ignoreException = true;
                }
                /*if (con.Request.UserAgent != null)
                {
                    body += "<br>User Agent: " + con.Request.UserAgent;
                }*/
            }
            body += "<br><br>" + DateTime.UtcNow.ToString();

            //if (exp is System.Web.HttpRequestValidationException || exceptionObject is System.Web.UI.ViewStateException) ignoreException = true;

            if (!ignoreException)
            {
                if (enableNotifications)
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
                    auditEntry.User = dataModel.Users.First(u => u.Id == (int)StandardUsers.System);
                }
                else
                {
                    auditEntry.UserId = user.ID;
                }

                auditEntry.EventDescription = "[" + eventType.ToString() + "]:" + (eventDescription != null ? eventDescription : "");
                auditEntry.Comment = comment;
                auditEntry.EventDate = DateTime.UtcNow;

                dataModel.AuditLogs.Add(auditEntry);
                dataModel.SaveChanges();

                System.Diagnostics.Debug.WriteLine("Log:" + auditEntry.EventDescription);
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
                    && (userID == null || (userID != null && a.UserId == userID))
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