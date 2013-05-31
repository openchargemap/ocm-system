using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;
using System.Net.Mail;
using System.Net;
using System.Configuration;


namespace OCM.API.Common
{
    public enum NotificationType
    {
        LocationSubmitted = 10,        
        LocationCommentReceived = 20,
        ContactUsMessage = 200,
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

        List<NotificationSetting> Settings { get; set; }
        public NotificationManager()
        {
            Settings = new List<NotificationSetting>();
            //load notification settings
            try
            {
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
                        Subject = Subject.Replace("{" + key + "}", templateParams[key].ToString());
                        MessageBody = MessageBody.Replace("{" + key + "}", templateParams[key].ToString());
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
                smtp.Send(mail);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message);
            }

            return false;
        }
    }
}