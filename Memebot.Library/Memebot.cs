using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using RedditSharp;
using RedditSharp.Things;

namespace Memebot.Library
{
    public static class Memebot
    {
        /// <summary>
        /// Creates Reddit object and logs in automatically from Key Vault
        /// </summary>
        /// <returns>Logged-in Reddit object</returns>
        public static Reddit CreateReddit()
        {
            KeyVaultHelper.LogIntoKeyVault();

            #region Secrets
            string redditUsername = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-username/");
            string redditPassword = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-password/");
            string redditClientID = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-client-id/");
            string redditClientSecret = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/reddit-secret/");
            string redditRedirectURI = "https://memebot-hackucf.azurewebsites.net/";
            #endregion

            var webAgent = new BotWebAgent(redditUsername, redditPassword, redditClientID, redditClientSecret, redditRedirectURI);
            
            return new Reddit(webAgent, false);
        }

        /// <summary>
        /// Collects top x memes from given each subreddit and stores them in Azure Queue
        /// </summary>
        /// <param name="numMemesPerSubreddit">Number of top memes per subreddit to post</param>
        /// <param name="subredditList">List of strings of subreddit names like /r/subreddit</param>
        /// <param name="reddit">logged-in Reddit object</param>
        /// <param name="storageConnectionString">Connection string for Azure Queue Storage</param>
        public static void CollectTopMemesToQueue(int numMemesPerSubreddit, IEnumerable<string> subredditList, Reddit reddit, string storageConnectionString)
        {
            // accessing Azure Queue Storage
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference("memes");

            // go through each subreddit in the list
            foreach(var subredditName in subredditList)
            {
                try
                {
                    // fetch subreddit
                    var subreddit = reddit.GetSubredditAsync(subredditName).Result;

                    // get top posts of the day
                    var topPosts = subreddit.GetTop(FromTime.Day, numMemesPerSubreddit);

                    // enumerate the top posts
                    var topPostsEnumerator = topPosts.GetEnumerator();
                    CancellationTokenSource source = new CancellationTokenSource();

                    // cycle through the top posts in the subreddit
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
    

        
        /// <summary>
        /// Will take meme from Azure Queue Storage and post to Slack
        /// </summary>
        /// <param name="webhookUrl">webhook url from Slack</param>
        /// <param name="numMemes">number of memes from the stack to post</param>
        /// <param name="storageConnectionString">Storage connection string for Azure Queue Storage</param>
        public static void PostToSlackFromQueue(int numMemes, string webhookUrl, string storageConnectionString)
        {
            HttpClient client = new HttpClient();

            // accessing Azure Queue Storage
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference("memes");

            // cycle through number of memes to post from the queue
            for(int i = 0; i < numMemes; i++)
            {
                // grab message from the queue
                var message = queue.GetMessage();

                // delete message from the Azure queue
                if(message == null){ break; }

                // save the url from the queue message
                var memeUrl = message.AsString;

                // delete message from queue now that we have the url
                queue.DeleteMessage(message);

                // create a json payload to POST to Slack
                var jsonPayload = JsonConvert.SerializeObject(new {text = memeUrl});
                var stringContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // POST to Slack via the webhook URL
                client.PostAsync(webhookUrl, stringContent).Wait();
            }
        }
    }
}
