using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OCM.MVC
{
    public class CustomAuthOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "Custom";
        public string Scheme => DefaultScheme;
    }

    public class CustomAuthHandler : AuthenticationHandler<CustomAuthOptions>
    {
        public CustomAuthHandler(IOptionsMonitor<CustomAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {

            if (Request.HttpContext.Session.GetInt32("UserID") != null)
            {

                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.Name, Request.HttpContext.Session.GetString("Username")));

                if (Request.HttpContext.Session.GetString("IsAdministrator") != null && bool.Parse(Request.HttpContext.Session.GetString("IsAdministrator")) == true)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                    claims.Add(new Claim(ClaimTypes.Role, "StandardUser"));
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, "StandardUser"));
                }

                var id = new ClaimsIdentity(claims);
                var identities = new List<ClaimsIdentity> { id };

                var cp = new ClaimsPrincipal(identities);
                var ticket = new AuthenticationTicket(cp, Options.Scheme);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            else
            {
                // user not authenticated
                return Task.FromResult(AuthenticateResult.Fail("Not signed in"));
            }
        }
    }

    public class IsUserAdminRequirement : IAuthorizationRequirement
    {
    }
    public class IsUserSignedInRequirement : IAuthorizationRequirement
    {
    }
    public class IsUserAdminRequirementHandler : AuthorizationHandler<IsUserAdminRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;
        public IsUserAdminRequirementHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                  IsUserAdminRequirement requirement)
        {

            if (_session.GetString("IsAdministration") != null && bool.Parse(_session.GetString("IsAdministration")) == true)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }


            return Task.CompletedTask;

        }
    }
    public class IsUserSignedInRequirementHandler : AuthorizationHandler<IsUserSignedInRequirement>, IAuthorizationRequirement
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;
        public IsUserSignedInRequirementHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                  IsUserSignedInRequirement requirement)
        {

            if (_session.GetInt32("UserID") != null)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;

        }
    }
}

