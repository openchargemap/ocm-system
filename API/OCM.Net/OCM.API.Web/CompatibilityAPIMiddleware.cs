using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OCM.Core.Settings;
using System.Threading.Tasks;
namespace OCM.API.Web.Standard
{

    public class CompatibilityAPIMiddleware
    {
        private readonly RequestDelegate _next;

        private CoreSettings _settings;

        public CompatibilityAPIMiddleware(RequestDelegate next, IConfiguration config)
        {
            _settings = new CoreSettings();
            config.GetSection("CoreSettings").Bind(_settings);

            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Do something with context near the beginning of request processing.
            if (context.Request.Path.ToString() == "/favicon.ico"){
                return;
            }
            
            if (!context.Request.Path.ToString().StartsWith("/v4/"))
            {
                var handled = await new CompatibilityAPICoreHTTPHandler(_settings).ProcessRequest(context);

                if (!handled)
                {
                    // call next middleware, API controllers etc
                    await _next.Invoke(context);
                }
            }
            else
            {
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

