using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;

namespace SSMVCCoreApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    if (!hostingContext.HostingEnvironment.IsDevelopment())
                    {
                        SetupKeyVault(hostingContext, config);
                    }
                    else if (hostingContext.HostingEnvironment.IsDevelopment())
                    {
                        SetupKeyVault(hostingContext, config);
                    }
                })
                .UseStartup<Startup>();

        private static void SetupKeyVault(WebHostBuilderContext hostingContext, IConfigurationBuilder config)
        {
            var buildConfig = config.Build();
            var keyVaultEndPoint = buildConfig["SSKeyVault"];
            if (!string.IsNullOrWhiteSpace(keyVaultEndPoint))
            {
                var azureTokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureTokenProvider.KeyVaultTokenCallback));
                config.AddAzureKeyVault(keyVaultEndPoint, keyVaultClient, new DefaultKeyVaultSecretManager());
            }
        }
    }
}
