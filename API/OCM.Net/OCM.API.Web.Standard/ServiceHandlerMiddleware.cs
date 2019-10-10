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
            await new CompatibilityAPICoreHTTPHandler().ProcessRequest(context);

            // call next middleware
            //await _next.Invoke(context);

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

