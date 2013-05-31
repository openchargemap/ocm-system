using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using OCM.API.Common;

namespace OCM.API.Test
{
    public partial class MailTest : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            NotificationManager notification = new NotificationManager();
            
            notification.PrepareNotification(NotificationType.ContactUsMessage, null);
            
            notification.SendNotification(NotificationType.ContactUsMessage);
        }
    }
}