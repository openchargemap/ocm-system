using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System;
using System.Timers;
using System.ComponentModel;

namespace OCM.API.Web.Standard
{
    public class Program
    {
        private static System.Timers.Timer customGarabageCollectionTimer;

        public static void Main(string[] args)
        {
            customGarabageCollectionTimer = new System.Timers.Timer(60 * 1000);
            customGarabageCollectionTimer.Elapsed += (Object source, ElapsedEventArgs e) =>
            {
                try
                {
                    Debug.WriteLine("Memory used before collection:       {0:N0}",
                         GC.GetTotalMemory(false));

                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

                    Debug.WriteLine("Memory used after collection:       {0:N0}",
                        GC.GetTotalMemory(false));
                }
                catch (Exception exp)
                {
                    Debug.WriteLine(exp);
                }
            };

            customGarabageCollectionTimer.AutoReset = true;
            customGarabageCollectionTimer.Enabled = true;

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
