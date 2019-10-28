using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
namespace OCM.API.Web.Standard
{

    public class CompatibilityAPIMiddleware
    {
        private readonly RequestDelegate _next;

        public CompatibilityAPIMiddleware(RequestDelegate next)
        {
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
                await new CompatibilityAPICoreHTTPHandler().ProcessRequest(context);
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

