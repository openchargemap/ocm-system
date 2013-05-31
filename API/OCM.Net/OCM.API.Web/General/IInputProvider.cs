using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using OCM.API.Common.Model;
using OCM.API.Common;

namespace OCM.API.InputProviders
{
    interface IInputProvider
    {
        bool ProcessEquipmentSubmission(HttpContext context, ref OCM.API.Common.Model.ChargePoint cp);
        bool ProcessUserCommentSubmission(HttpContext context, ref OCM.API.Common.Model.UserComment comment);
        bool ProcessContactUsSubmission(HttpContext context, ref ContactSubmission comment);
        User GetUserFromAPICall(HttpContext context);
    }
}
