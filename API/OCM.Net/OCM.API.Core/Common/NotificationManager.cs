using Mandrill;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;

namespace OCM.API.Common
{
    public enum NotificationType
    {
        LocationSubmitted = 10,
        LocationCommentReceived = 20,
        ContactUsMessage = 200,
        SubscriptionNotification = 300,
        FaultReport = 1000
    }

    public class NotificationSetting
    {
        public NotificationType NotificationType { get; set; }

        public string Subject { get; set; }

        public string TemplateFile { get; set; }
    }

    /// <summary>
    /// Summary description for NotificationManager
    /// </summary>
    public class NotificationManager
    {
        public string MessageBody { get; set; }

        public string Subject { get; set; }

        public bool SendViaAPI { get; set; }

        private List<NotificationSetting> Settings { get; set; }

        public NotificationManager()
        {
            SendViaAPI = false;
            Settings = new List<NotificationSetting>();
            //load notification settings
            try
            {
                SendViaAPI = bool.Parse(ConfigurationManager.AppSettings["Notifications_SendViaAPI"]);

                string[] configVals = ConfigurationManager.AppSettings["NotificationSetting_ContactUs"].ToString().Split(';');
                Settings.Add(new NotificationSetting
                {
                    NotificationType = NotificationType.ContactUsMessage,
                    TemplateFile = configVals[0],
                    Subject = configVals[1]
                });

                configVals = ConfigurationManager.AppSettings["NotificationSetting_LocationSubmitted"].ToString().Split(';');
                Settings.Add(new NotificationSetting
                {
                    NotificationType = NotificationType.LocationSubmitted,
                    TemplateFile = configVals[0],
                    Subject = configVals[1]
                });

                configVals = ConfigurationManager.AppSettings["NotificationSetting_LocationCommentReceived"].ToString().Split(';');
                Settings.Add(new NotificationSetting
                {
                    NotificationType = NotificationType.LocationCommentReceived,
                    TemplateFile = configVals[0],
                    Subject = configVals[1]
                });

                configVals = ConfigurationManager.AppSettings["NotificationSetting_FaultReport"].ToString().Split(';');
                Settings.Add(new NotificationSetting
                {
                    NotificationType = NotificationType.FaultReport,
                    TemplateFile = configVals[0],
                    Subject = configVals[1]
                });

                configVals = ConfigurationManager.AppSettings["NotificationSetting_SubscriptionNotification"].ToString().Split(';');
                Settings.Add(new NotificationSetting
                {
                    NotificationType = NotificationType.SubscriptionNotification,
                    TemplateFile = configVals[0],
                    Subject = configVals[1]
                });
            }
            catch (Exception)
            {
                ; ; //failed to load notification settings
            }
        }

        public void PrepareNotification(NotificationType notificationType, Hashtable templateParams)
        {
            string templateFolder = HttpContext.Current.Server.MapPath("~/templates/notifications");
            string BaseTemplate = System.IO.File.ReadAllText(templateFolder + "\\BaseTemplate.htm");

            MessageBody = BaseTemplate;

            var notificationSettings = Settings.FirstOrDefault(s => s.NotificationType == notificationType);

            if (notificationSettings != null)
            {
                //load message template and replace template placeholder keys with param values
                string messageTemplate = System.IO.File.ReadAllText(templateFolder + "\\" + notificationSettings.TemplateFile);
                this.Subject = notificationSettings.Subject;
                MessageBody = MessageBody.Replace("{MessageBody}", messageTemplate);

                if (templateParams != null)
                {
                    foreach (string key in templateParams.Keys)
                    {
                        if (templateParams[key] != null)
                        {
                            Subject = Subject.Replace("{" + key + "}", templateParams[key].ToString());
                            MessageBody = MessageBody.Replace("{" + key + "}", templateParams[key].ToString());
                        }
                    }
                }
            }
            else
            {
                MessageBody = MessageBody.Replace("{MessageBody}", "");
            }
        }

        public bool SendNotification(string toEmail)
        {
            return SendNotification(toEmail, null);
        }

        //Send notification to default recipients (sys admin)
        public bool SendNotification(NotificationType recipientType)
        {
            string defaultRecipients = ConfigurationManager.AppSettings["DefaultRecipientEmailAddresses"].ToString();
            string customRecipients = ConfigurationManager.AppSettings["DefaultRecipientEmailAddresses_" + recipientType.ToString()];
            if (!String.IsNullOrEmpty(customRecipients)) defaultRecipients = customRecipients;

            return SendNotification(defaultRecipients, null);
        }

        public bool SendNotification(string toEmail, string bccEmail)
        {
            bool isDebugOnly = false;
            try
            {
                MailMessage mail = new MailMessage();

                if (ConfigurationManager.AppSettings["DebugEmailAddress"] != "")
                {
                    toEmail = ConfigurationManager.AppSettings["DebugEmailAddress"].ToString();
                    isDebugOnly = true;
                }
                if (!SendViaAPI)
                {
                    //send via standard SMTP
                    //split list of addresses
                    string[] addresses = toEmail.Split(';');

                    foreach (string emailAddress in addresses)
                    {
                        mail.To.Add(emailAddress);
                    }

                    if (!String.IsNullOrEmpty(bccEmail) && !isDebugOnly)
                    {
                        addresses = bccEmail.Split(';');
                        foreach (string emailAddress in addresses)
                        {
                            mail.Bcc.Add(emailAddress);
                        }
                    }

                    mail.From = new MailAddress(ConfigurationManager.AppSettings["DefaultSenderEmailAddress"], "openchargemap.org - automated notification");
                    mail.Subject = this.Subject;

                    AlternateView htmlView = AlternateView.CreateAlternateViewFromString(MessageBody, null, "text/html");

                    mail.AlternateViews.Add(htmlView);
                    SmtpClient smtp = new SmtpClient();
                    smtp.Host = System.Configuration.ConfigurationManager.AppSettings["SMTPHost"];
                    if (!String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["SMTPUser"]))
                    {
                        NetworkCredential basicCredential = new NetworkCredential(ConfigurationManager.AppSettings["SMTPUser"], ConfigurationManager.AppSettings["SMTPPwd"]);
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = basicCredential;

                        //smtp.DeliveryMethod = SmtpDeliveryMethod.PickupDirectoryFromIis;
                        //smtp.Port = 587;
                    }
                    //smtp.DeliveryMethod = SmtpDeliveryMethod.PickupDirectoryFromIis;
                    try
                    {
                        smtp.Send(mail);
                    }
                    catch (Exception)
                    {
                        ; ;// failed to send
                        return false;
                    }

                    return true;
                }
                else
                {
                    //send via bulk mailing api
                    var apiKey = System.Configuration.ConfigurationManager.AppSettings["MandrillAPIKey"];
                    if (!String.IsNullOrEmpty(apiKey))
                    {
                        MandrillApi api = new MandrillApi(apiKey);
                        var message = new EmailMessage();

                        string[] addresses = toEmail.Split(';');
                        var emailToList = new List<EmailAddress>();
                        foreach (string emailAddress in addresses)
                        {
                            emailToList.Add(new EmailAddress(emailAddress));
                        }
                        message.to = emailToList;

                        if (!String.IsNullOrEmpty(bccEmail) && !isDebugOnly)
                        {
                            addresses = bccEmail.Split(';');
                            var bccToList = new List<EmailAddress>();
                            foreach (string emailAddress in addresses)
                            {
                                bccToList.Add(new EmailAddress(emailAddress));
                            }
                        }

                        message.from_email = ConfigurationManager.AppSettings["DefaultSenderEmailAddress"];
                        message.from_name = "Open Charge Map - Automated Notification";
                        message.subject = this.Subject;

                        message.auto_text = true;
                        message.html = this.MessageBody;
                        var result = api.SendMessage(message);
                        if (result != null && result.Any())
                        {
                            //optional notification result logging
                            logEvent(JSON.Serialize(new { eventDate = DateTime.UtcNow, result = result }));

                            var r = result.First();
                            if (r.Status == EmailResultStatus.Invalid || r.Status == EmailResultStatus.Rejected)
                            {
                                return false;
                            }
                        }
                        else
                        {
                            logEvent(JSON.Serialize(new { eventDate = DateTime.UtcNow, result = result }));
                        }

                        return true;
                    }
                   
                }
            }
            catch (Exception ex)
            {
                logEvent(JSON.Serialize(new { eventDate = DateTime.UtcNow, result = ex }));
            }

            return false;
        }

        private void logEvent(string content)
        {
            try
            {
                if (bool.Parse(ConfigurationManager.AppSettings["EnableNotificationLogging"]) == true)
                {
                    string logPath = ConfigurationManager.AppSettings["CachePath"] + "\\notifications.log";
                    System.IO.File.AppendAllText(logPath, content);
                }
            }
            catch (Exception) { }
        }
    }
}