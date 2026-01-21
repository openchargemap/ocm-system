using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.API.InputProviders;
using OCM.Core.Data;
using System.Linq;

namespace OCM.API.Web.Standard.Controllers
{
    [ApiController]
    [Route("/v4/[controller]")]
    public class MediaItemController : ControllerBase
    {
        private readonly ILogger _logger;

        public MediaItemController(ILogger<MediaItemController> logger)
        {
            _logger = logger;
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var apiKey = Request.Query["apikey"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Unauthorized();
            }

            var user = new InputProviderBase().GetUserFromAPICall(HttpContext, apiKey);
            if (user == null)
            {
                return Unauthorized();
            }

            using (var dataModel = new OCMEntities())
            {
                var item = dataModel.MediaItems.FirstOrDefault(m => m.Id == id);
                if (item == null)
                {
                    return NotFound();
                }

                bool canModerate = UserManager.IsUserAdministrator(user) || UserManager.HasUserPermission(user, null, PermissionLevel.Editor);
                if (item.UserId != user.ID && !canModerate)
                {
                    return Forbid();
                }
            }

            if (!new MediaItemManager().SoftDeleteMediaItem(user.ID, id))
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
