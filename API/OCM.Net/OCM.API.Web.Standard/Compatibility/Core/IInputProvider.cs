using Microsoft.AspNetCore.Http;
using OCM.API.Common;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace OCM.API.InputProviders
{
    internal interface IInputProvider
    {
        ValidationResult ProcessEquipmentSubmission(HttpContext context, ref OCM.API.Common.Model.ChargePoint cp);

        bool ProcessUserCommentSubmission(HttpContext context, ref OCM.API.Common.Model.UserComment comment);

        bool ProcessContactUsSubmission(HttpContext context, ref ContactSubmission comment);

        bool ProcessMediaItemSubmission(string uploadPath, HttpContext context, ref MediaItem mediaItem, int userId);

        User GetUserFromAPICall(HttpContext context);
    }
}