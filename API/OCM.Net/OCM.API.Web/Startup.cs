using Ben.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace OCM.API.Web.Standard
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            // use config to init the default caching provider instance
            var settings = new Core.Settings.CoreSettings();
            configuration.GetSection("CoreSettings").Bind(settings);

            if (settings.MongoDBSettings == null)
            {
                throw new Exception("OCM.API: Service Cannot Start, appsettings.json not found from current path.");
            }

            Core.Data.CacheProviderMongoDB.CreateDefaultInstance(settings);


            Core.Data.CacheManager.InitCaching(settings);

        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddResponseCompression();
            services.AddMemoryCache();

            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
#if DEBUG
            app.UseBlockingDetection();
#endif

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseStaticFiles();

            app.UseResponseCompression();

            app.UseHttpsRedirection();

            // provide handlers for compatibility with older API calls
            app.UseCompatibilityAPIMiddleware();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }
    }
}
