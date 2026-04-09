using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OCM.Import;
using System;

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
                        configurationBuilder.AddAzureKeyVault(
                            new Uri(importSettings.KeyVaultUri),
                            new ClientSecretCredential(
                                importSettings.KeyVaultTenantId,
                                importSettings.KeyVaultClientId,
                                importSettings.KeyVaultSecret));
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
