using Microsoft.AspNetCore.Http;
using OCM.API.Common;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.InputProviders
{
    internal interface IInputProvider
    {
        ValidationResult ProcessEquipmentSubmission(HttpContext context, ref OCM.API.Common.Model.ChargePoint cp);

        Task<bool> ProcessUserCommentSubmission(HttpContext context, OCM.API.Common.Model.UserComment comment);

        bool ProcessContactUsSubmission(HttpContext context, ref ContactSubmission comment);

        Task<bool> ProcessMediaItemSubmission(string uploadPath, HttpContext context, MediaItem mediaItem, int userId);

        User GetUserFromAPICall(HttpContext context);
    }
}