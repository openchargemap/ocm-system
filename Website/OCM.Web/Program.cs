using System;
using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OCM.Import;

namespace OCM.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, configurationBuilder) =>
                {
                    var configuration = configurationBuilder.Build();
                    var importSettings = new ImportSettings();
                    configuration.GetSection("ImportSettings").Bind(importSettings);

                    if (!string.IsNullOrWhiteSpace(importSettings.KeyVaultUri)
                        && !string.IsNullOrWhiteSpace(importSettings.KeyVaultTenantId)
                        && !string.IsNullOrWhiteSpace(importSettings.KeyVaultClientId)
                        && !string.IsNullOrWhiteSpace(importSettings.KeyVaultSecret))
                    {
                        try
                        {
                            configurationBuilder.AddAzureKeyVault(
                                new Uri(importSettings.KeyVaultUri),
                                new ClientSecretCredential(
                                    importSettings.KeyVaultTenantId,
                                    importSettings.KeyVaultClientId,
                                    importSettings.KeyVaultSecret));
                        }
                        catch (Exception)
                        {
                            System.Diagnostics.Debug.WriteLine("Exception adding keyvault. Azure credential may have expired");
                        }
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
