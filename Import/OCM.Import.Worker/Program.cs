using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OCM.Import.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                        .ConfigureAppConfiguration(c =>
                        {
                            var config = c.Build();
                            
                            var settings = new ImportSettings();

                            config.GetSection("ImportSettings").Bind(settings);
                            
                            var keyvaultUri = new System.Uri(settings.KeyVaultUri);

                            c.AddAzureKeyVault(keyvaultUri, new ClientSecretCredential(settings.KeyVaultTenantId, settings.KeyVaultClientId, settings.KeyVaultSecret));
                            
                        })
                        .UseSystemd()
                        .ConfigureServices((hostContext, services) =>
                        {
                            services.AddHostedService<Worker>();
                        });


    }
}
