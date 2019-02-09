using System;
using RedditSharp;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace Memebot.Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            KeyVaultHelper.LogIntoKeyVault();


            string redditUsername = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-username/");
            string redditPassword = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-password/");
            string redditClientID = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-client-id/");
            string redditClientSecret = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-secret/");
            string redditRedirectURI = "https://memebot-hackucf.azurewebsites.net/";

            var webAgent = new BotWebAgent(redditUsername, redditPassword, redditClientID, redditClientSecret, redditRedirectURI);
            var reddit = new Reddit(webAgent, false);
        }
    }

    public class KeyVaultHelper
    {
        /// <summary>
        /// Key Vault client that can get keys and secrets
        /// </summary>
        /// <value></value>
        public static KeyVaultClient keyVaultClient { get; private set; }

        /// <summary>
        /// Logs into Azure KeyVault and makes keyVaultClient active
        /// </summary>
        public static void LogIntoKeyVault()
        {
            // authenticating with Azure Managed Service Identity
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();

            keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
        }

        /// <summary>
        /// Gets secret value from Azure KeyVault
        /// </summary>
        /// <param name="secretIdentifier">URI found in the Azure portal that identifies a secret</param>
        /// <returns>string value of secret</returns>
        public static string GetSecret(string secretIdentifier)
        {
            return keyVaultClient.GetSecretAsync(secretIdentifier).Result.Value;
        }
    }
}
