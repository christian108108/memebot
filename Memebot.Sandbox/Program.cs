using System;
using RedditSharp;
using RedditSharp.Things;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using System.Threading;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;

namespace Memebot.Sandbox
{
    class Program
    {

        private static readonly HttpClient client = new HttpClient();
        
        static void Main(string[] args)
        {
            KeyVaultHelper.LogIntoKeyVault();

            #region Secrets
            string redditUsername = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-username/");
            string redditPassword = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-password/");
            string redditClientID = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-client-id/");
            string redditClientSecret = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-secret/");
            string redditRedirectURI = "https://memebot-hackucf.azurewebsites.net/";
            string slackWebhookUrl = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/slack-webhook-url/");
            #endregion

            var webAgent = new BotWebAgent(redditUsername, redditPassword, redditClientID, redditClientSecret, redditRedirectURI);
            var reddit = new Reddit(webAgent, false);

            // spiciest memes around
            var subredditList = new List<string>()
            {
                "/r/prequelmemes",
                "/r/4chan",
                "/r/animemes"
            };

            // go through each subreddit in the list
            foreach(var subredditName in subredditList)
            {
                var subreddit = reddit.GetSubredditAsync(subredditName).Result;

                // get top 2 posts of the day
                var topFivePosts = subreddit.GetTop(FromTime.Day, 2);

                // iterate through the top 2 posts
                var topFivePostsEnumerator = topFivePosts.GetEnumerator();

                CancellationTokenSource source = new CancellationTokenSource();

                while(topFivePostsEnumerator.MoveNext(source.Token).Result)
                {
                    var currentPost = topFivePostsEnumerator.Current;

                    // get full URL for the reddit post
                    var imageUrl = "https://www.reddit.com" + currentPost.Permalink.OriginalString;

                    PostMeme(imageUrl, slackWebhookUrl);
                    ;
                }

            }

            ;
        }

        /// <summary>
        /// Will post meme to a Slack webhook
        /// </summary>
        /// <param name="memeUrl">full url of the meme</param>
        /// <param name="webhookUrl">webhook url from Slack</param>
        /// <returns></returns>
        public static bool PostMeme(string memeUrl, string webhookUrl)
        {
            var jsonPayload = JsonConvert.SerializeObject(new {text = memeUrl});

            var stringContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            return client.PostAsync(webhookUrl, stringContent).Result.IsSuccessStatusCode;
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
