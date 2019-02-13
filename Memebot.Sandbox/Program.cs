using RedditSharp;
using RedditSharp.Things;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using System.Threading;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Memebot.Sandbox
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        
        static void Main(string[] args)
        {
            // spiciest memes around
            string[] subredditList = 
            {
                "/r/prequelmemes",
                "/r/4chan",
                "/r/animemes",
                "/r/blackpeopletwitter",
                "/r/bikinibottomtwitter"
            };

            // collect memes and store in Azure Queue Storage
            // CollectTopMemes(2, subredditList);

            // post memes from Azure Queue Storage into Slack
            KeyVaultHelper.LogIntoKeyVault();
            var slackWebhookUrl = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/slack-webhook-url/");
            PostToSlack(2, slackWebhookUrl);


            ;
        }

        /// <summary>
        /// Will take meme from Azure Queue Storage and post to Slack
        /// </summary>
        /// <param name="webhookUrl">webhook url from Slack</param>
        /// <param name="numMemes">number of memes from the stack to post</param>
        public static void PostToSlack(int numMemes, string webhookUrl)
        {
            // log into Key Vault and grab storage connection string
            KeyVaultHelper.LogIntoKeyVault();
            string storageConnectionString = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/storage-connection-string/");

            // accessing Azure Queue Storage
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference("memes");

            // cycle through number of memes to post from the queue
            for(int i = 0; i < numMemes; i++)
            {
                var message = queue.GetMessage();

                // delete message from the Azure queue
                if(message != null){ queue.DeleteMessage(message); }

                var memeUrl = message.AsString;

                if(String.IsNullOrWhiteSpace(memeUrl)) { break; }

                var jsonPayload = JsonConvert.SerializeObject(new {text = memeUrl});

                var stringContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.PostAsync(webhookUrl, stringContent).Wait();
            }

        }

        /// <summary>
        /// Posts top x memes from given each subreddit and stores them in Azure Queue
        /// </summary>
        /// <param name="numMemesPerSubreddit">Number of top memes per subreddit to post</param>
        /// <param name="subredditList">List of strings of subreddit names like /r/subreddit</param>
        public static void CollectTopMemes(int numMemesPerSubreddit, IEnumerable<string> subredditList)
        {
            KeyVaultHelper.LogIntoKeyVault();

            #region Secrets
            string storageConnectionString = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/storage-connection-string/");
            string redditUsername = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-username/");
            string redditPassword = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-password/");
            string redditClientID = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-client-id/");
            string redditClientSecret = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-secret/");
            string redditRedirectURI = "https://memebot-hackucf.azurewebsites.net/";
            #endregion

            var webAgent = new BotWebAgent(redditUsername, redditPassword, redditClientID, redditClientSecret, redditRedirectURI);
            var reddit = new Reddit(webAgent, false);

            // accessing Azure Queue Storage
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference("memes");

            // go through each subreddit in the list
            foreach(var subredditName in subredditList)
            {
                try
                {
                    var subreddit = reddit.GetSubredditAsync(subredditName).Result;

                    // get top posts of the day
                    var topPosts = subreddit.GetTop(FromTime.Day, numMemesPerSubreddit);

                    // iterate through the top posts
                    var topPostsEnumerator = topPosts.GetEnumerator();
                    CancellationTokenSource source = new CancellationTokenSource();

                    while(topPostsEnumerator.MoveNext(source.Token).Result)
                    {
                        var currentPost = topPostsEnumerator.Current;

                        // get full URL for the reddit content
                        var imageUrl = currentPost.Url.AbsoluteUri;

                        // create message and adds to queue
                        var message = new CloudQueueMessage(imageUrl, false);
                        queue.AddMessage(message);
                    }
                }
                catch
                {
                    Console.WriteLine("Could not find subreddit");
                }
            }
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
            try
            {
                return keyVaultClient.GetSecretAsync(secretIdentifier).Result.Value;
            }
            catch
            {
                throw new UnauthorizedAccessException("Please log into Azure CLI and try again. Try typing command 'az login'");
            }
        }
    }
}