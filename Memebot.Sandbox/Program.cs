using System;
using RedditSharp;
using RedditSharp.Things;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using System.Threading;

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

            var subreddit = reddit.GetSubredditAsync("/r/prequelmemes").Result;

            // get top 5 posts of the day
            var topFivePosts = subreddit.GetTop(FromTime.Day);
            
            // iterate through the top 5 posts
            var topFivePostsEnumerator = topFivePosts.GetEnumerator();

            CancellationTokenSource source = new CancellationTokenSource();

            int numMemes = 0;
            while(topFivePostsEnumerator.MoveNext(source.Token).Result)
            {
                var currentPost = topFivePostsEnumerator.Current;

                var imageUrl = currentPost.Url.AbsoluteUri;

                var fileExtension = GetFileExtension(imageUrl);

                if( fileExtension == "jpg" ||
                    fileExtension == "jpeg" ||
                    fileExtension == "gif" ||
                    fileExtension == "png")
                {
                    var wc = new System.Net.WebClient();
                    wc.DownloadFile( topFivePostsEnumerator.Current.Url, $"download.{fileExtension}");
                    numMemes++;
                }

                if(numMemes == 5)
                {
                    source.Cancel();
                    break;
                }
            }

            ;
        }

        public static string GetFileExtension(string fileUri)
        {
            int lastDot = fileUri.LastIndexOf('.');
            return fileUri.Substring(lastDot+1);
        }
    }

    public class KeyVaultHelper
    {
        /// <summary>
        /// Key Vault client that can get keys and secrets
        /// </summary>
        /// <value></value>
        private static KeyVaultClient keyVaultClient { get; set; }

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
