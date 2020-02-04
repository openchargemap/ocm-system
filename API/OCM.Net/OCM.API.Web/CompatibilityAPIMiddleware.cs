using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCM.Core.Settings;
using System.Threading.Tasks;
namespace OCM.API.Web.Standard
{

    public class CompatibilityAPIMiddleware
    {
        private readonly RequestDelegate _next;

        private CoreSettings _settings;
        private ILogger _logger;

        public CompatibilityAPIMiddleware(RequestDelegate next, IConfiguration config, ILogger<CompatibilityAPIMiddleware> logger)
        {
            _settings = new CoreSettings();
            config.GetSection("CoreSettings").Bind(_settings);

            _next = next;

            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            // Do something with context near the beginning of request processing.
            if (context.Request.Path.ToString() == "/favicon.ico"){
                return;
            }
            
            if (!context.Request.Path.ToString().StartsWith("/v4/") && !context.Request.Path.ToString().StartsWith("/map"))
            {
                var handled = await new CompatibilityAPICoreHTTPHandler(_settings, _logger).ProcessRequest(context);

                if (!handled)
                {
                    // call next middleware, API controllers etc
                    await _next.Invoke(context);
                }
            }
            else
            {
                _logger.LogDebug("Passing v4 request to controller..");

                // call next middleware, API controllers etc
                await _next.Invoke(context);
            }
            

            // Clean up.
            await context.Response.CompleteAsync();
        }
    }

    public static class CompatibilityAPIMiddlewareExtensions
    {
        public static IApplicationBuilder UseCompatibilityAPIMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CompatibilityAPIMiddleware>();
        }
    }
}

