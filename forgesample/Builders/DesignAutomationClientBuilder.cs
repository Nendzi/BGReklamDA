using Autodesk.Forge.Core;
using Autodesk.Forge.DesignAutomation;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace forgesample.Builders
{
    public class DesignAutomationClientBuilder
    {
        internal DesignAutomationClient Client { get; }

        public DesignAutomationClientBuilder(string forgeClientId, string forgeClientSecret)
        {
            Client = CreateDesignAutomationClient(forgeClientId, forgeClientSecret);
        }
        private DesignAutomationClient CreateDesignAutomationClient(string forgeClientId, string forgeClientSecret)
        {
            var forgeService = CreateForgeService(forgeClientId, forgeClientSecret);
            return new DesignAutomationClient(forgeService);
        }
        private ForgeService CreateForgeService(string forgeClientId, string forgeClientSecret)
        {
            var forgeConfig = new ForgeConfiguration();
            forgeConfig.ClientId = forgeClientId;
            forgeConfig.ClientSecret = forgeClientSecret;
            var httpMessageHandler = new ForgeHandler(Options.Create(forgeConfig))
            {
                InnerHandler = new HttpClientHandler()
            };
            return new ForgeService(new HttpClient(httpMessageHandler));
        }
    }
}