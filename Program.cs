using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Hosting;
using System;

namespace Azure_ServiceEndpoint_Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    if (context.HostingEnvironment.IsProduction())
                    {
                        // When deployed in Azure use the Application's System Managed Identity 
                        // to access a Key Vault for configuration/secret data.
                        // https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-3.1
                        // What the documentation does not make clear is that it will walk the configuration 
                        // logical path to find the final value. Search path is as follows
                        //      1 Azure Key Vault
                        //      2 Web Application configuration
                        //      3 appsettings.{Environment}.json - If available/appropriate
                        //      4 appsettings.json

                        var builtConfig = config.Build();

                        var azureServiceTokenProvider = new AzureServiceTokenProvider();
                        var keyVaultClient = new KeyVaultClient(
                            new KeyVaultClient.AuthenticationCallback(
                                azureServiceTokenProvider.KeyVaultTokenCallback));

                        string keyVaultURL = builtConfig["KeyVaultURL"];
                        int keyVaultRefreshSeconds = int.Parse(builtConfig["KeyVaultRefreshSeconds"]);

                        config.AddAzureKeyVault(
                            new AzureKeyVaultConfigurationOptions()
                            {
                                Client = keyVaultClient,
                                Manager = new DefaultKeyVaultSecretManager(),
                                ReloadInterval = new System.TimeSpan(0, 0, keyVaultRefreshSeconds),
                                Vault = keyVaultURL
                            });
                    }
                })
                .UseStartup<Startup>();
    }
}