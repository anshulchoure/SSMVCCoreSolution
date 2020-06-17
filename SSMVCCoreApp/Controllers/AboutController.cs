using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SSMVCCoreApp.Controllers
{
    public class AboutController : Controller
    {
        private readonly IConfiguration _configuration;
        public AboutController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<IActionResult> Index()
        {
            AzureServiceTokenProvider tokenProvider = new AzureServiceTokenProvider();
            KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));

            var secrets = await keyVaultClient.GetSecretsAsync(_configuration["SSKeyVault"]);

            Dictionary<string, string> secreteKeyValueList = new Dictionary<string, string>();
            foreach (var item in secrets)
            {
                var secret = await keyVaultClient.GetSecretAsync($"{item.Id}");
                secreteKeyValueList.Add(item.Id, secret.Value);
            }
            return View(secreteKeyValueList);
        }
        public IActionResult Throw()
        {
            throw new EntryPointNotFoundException("This is a user thrown exception");
        }
    }
}
