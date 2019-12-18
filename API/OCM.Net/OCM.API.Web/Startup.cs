using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphiQl;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCM.API.Web.Models.GraphQL;

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

            // graphql config, inject schema provider
            /* var configPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
             var schemaDefinition = System.IO.File.ReadAllText(configPath + "/Templates/GraphQL/poi-schema.gql");

             var schema = GraphQL.Types.Schema.For(schemaDefinition, _ =>
             {
                 _.Types.Include<Models.GraphQL.GraphQLQuery>();
             });
             services.AddSingleton<GraphQL.Types.ISchema>(schema);

             services.AddSingleton<GraphQL.IDocumentExecuter>(new GraphQL.DocumentExecuter());
             services.AddSingleton<GraphQL.Http.IDocumentWriter>(new GraphQL.Http.DocumentWriter(true));

             services.AddSingleton<GraphQL.IDependencyResolver>(s => new GraphQL.FuncDependencyResolver(s.GetRequiredService));*/

            services.AddSingleton<GraphQL.IDocumentExecuter>(new GraphQL.DocumentExecuter());
            services.AddSingleton<GraphQL.Http.IDocumentWriter>(new GraphQL.Http.DocumentWriter(true));

            services.AddSingleton<GraphQL.IDependencyResolver>(s => new GraphQL.FuncDependencyResolver(s.GetRequiredService));
            services.AddTransient<IPoiRepository, PoiRepository>();

            /////////////////
            var configPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var schemaDefinition = System.IO.File.ReadAllText(configPath + "/Templates/GraphQL/poi-schema.gql");

            var schema = GraphQL.Types.Schema.For(schemaDefinition, _ =>
            {
                _.Types.Include<Models.GraphQL.GraphQLQuery>();
          
            });
            services.AddSingleton<GraphQL.Types.ISchema>(schema);
            //////////////
            //services.AddSingleton<ISchema, PoiSchema>();
            services.AddSingleton<PoiQuery>();
            services.AddSingleton<PoiType>();
          //  services.AddGraphQL();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseResponseCompression();

            app.UseHttpsRedirection();

#if DEBUG
            app.UseGraphiQl("/graphql", "/v4/graphql");
#endif

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
