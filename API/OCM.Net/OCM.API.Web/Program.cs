using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace OCM.API.Web.Standard
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .ConfigureKestrel(serverOptions => serverOptions.AllowSynchronousIO = true) // allow sync IO for legacy outputs
                        .UseStartup<Startup>();
                });
    }
}
