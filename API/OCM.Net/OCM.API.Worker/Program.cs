using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OCM.API.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.AllowSynchronousIO = true;
                        serverOptions.UseSystemd();
                    }
                    ); ; // allow sync IO for legacy outputs
                    webBuilder.UseStartup<Startup>();
                });


    }
}
