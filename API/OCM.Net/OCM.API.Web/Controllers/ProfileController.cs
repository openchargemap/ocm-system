using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.API.Common.Model.Extended;

namespace OCM.API.Web.Standard.Controllers
{
    [ApiController]
    [Route("/v4/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly ILogger _logger;

        public ProfileController(ILogger<ProfileController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public ActionResult Authenticate(LoginModel login)
        {
           
            User user = new OCM.API.Common.UserManager().GetUser(login);
            string access_token = null;
            var responseEnvelope = new APIResponseEnvelope();
            if (user == null)
            {
                return Unauthorized();
                
            }
            else
            {
                access_token = Security.JWTAuth.GenerateEncodedJWT(user);
            }

            responseEnvelope.Data = new { UserProfile = user, access_token = access_token };
        
            return Ok(responseEnvelope);
        }
    }
}
