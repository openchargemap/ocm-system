using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OCM.MVC;

namespace OCM.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDataProtection().SetApplicationName("OCM.Web");

            services.AddDistributedSqlServerCache(options =>
            {
                options.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["OCMEntities"].ConnectionString;
                options.SchemaName = "dbo";
                options.TableName = "SessionState";
               
            });

            services.AddControllersWithViews()
                    .AddRazorRuntimeCompilation();
            
            services.AddRouting(options => options.LowercaseUrls = true);

            services.AddAuthentication(CustomAuthOptions.DefaultScheme)
                        .AddScheme<CustomAuthOptions, CustomAuthHandler>(CustomAuthOptions.DefaultScheme, CustomAuthOptions.DefaultScheme
                        , opts => { });

            //services.AddMemoryCache();

          

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.Cookie.HttpOnly = true;
                // Make the session cookie essential
                options.Cookie.IsEssential = true;
     
            });


            services.AddSingleton<IAuthorizationHandler, IsUserSignedInRequirementHandler>();
            services.AddSingleton<IAuthorizationHandler, IsUserAdminRequirementHandler>();

            services.AddAuthorization(options =>
            {

                options.AddPolicy("IsSignedIn",
                   policy => policy.Requirements.Add(new IsUserSignedInRequirement()));
                options.AddPolicy("IsAdmin",
                    policy => policy.Requirements.Add(new IsUserAdminRequirement()));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            app.UseSession();

            app.UseStatusCodePagesWithRedirects("~/Home/Error?code={0}");
#if DEBUG
            if (env.IsDevelopment())
            {
                
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("~/home/error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
               // app.UseHsts();
            }
#else
             app.UseExceptionHandler("/Home/Error");
#endif
            //app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
