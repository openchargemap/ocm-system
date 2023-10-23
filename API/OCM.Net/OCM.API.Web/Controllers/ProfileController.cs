using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OCM.API.Common.Model;
using OCM.API.Common.Model.Extended;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OCM.API.Web.Standard.Controllers
{
    [ApiController]
    [Route("v4/[controller]/[action]")]
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

        [HttpPost]
       
        public ActionResult Register([FromBody]LoginModel registration)
        {
            if (string.IsNullOrWhiteSpace(registration.EmailAddress))
            {
                return BadRequest();
            }

            string access_token = null;
            var responseEnvelope = new APIResponseEnvelope();

            var userManager = new OCM.API.Common.UserManager();

            if (!userManager.IsExistingUser(registration.EmailAddress))
            {
                // credentials don't match existing user
                var user = userManager.RegisterNewUser(new RegistrationModel {  EmailAddress=registration.EmailAddress, Password=registration.Password, Username = registration.Username});

                if (user == null)
                {
                    return BadRequest();

                }
                else
                {
                    access_token = Security.JWTAuth.GenerateEncodedJWT(user);
                    responseEnvelope.Data = new { UserProfile = user, access_token = access_token };

                    return Ok(responseEnvelope);
                }
            }
            else
            {
                return Authenticate(new LoginModel { EmailAddress = registration.EmailAddress, Password = registration.Password });
            }
        }

        private static bool IsValidEmail(string email)
        {
            // https://learn.microsoft.com/en-us/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                return false;
            }
            catch (ArgumentException e)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}
