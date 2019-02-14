using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Memebot.Library;

namespace Memebot.Functions
{
    public static class PostMemesToSlack
    {
        [FunctionName("PostMemesToSlack")]
        public static void Run([TimerTrigger("0 0 */4 * * *")]TimerInfo myTimer, ILogger log)
        {
            // log into KeyVault and get secrets
            KeyVaultHelper.LogIntoKeyVault();
            var storageConnectionString = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/storage-connection-string/");
            var slackWebhookUrl = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/slack-webhook-url/");


            // post memes from Azure Queue Storage into Slack
            int numMemes = 2;
            Library.Memebot.PostToSlackFromQueue(numMemes, slackWebhookUrl, storageConnectionString);
            
            // logs back to Azure
            log.LogInformation($"Posted {numMemes} to Slack at: {DateTime.Now}");
        }
    }
    public static class MemebotCollector
    {
        [FunctionName("MemebotCollector")]
        public static void Run([TimerTrigger("0 0 0 * * *")]TimerInfo myTimer, ILogger log)
        {
            // spiciest memes around
            string[] subredditList = 
            {
                "/r/prequelmemes",
                "/r/4chan",
                "/r/animemes",
                "/r/blackpeopletwitter",
                "/r/bikinibottomtwitter",
            };

            // log into KeyVault and get secrets
            KeyVaultHelper.LogIntoKeyVault();
            var storageConnectionString = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/storage-connection-string/");
            var slackWebhookUrl = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/slack-webhook-url/");


            // collect memes and store in Azure Queue Storage
            var reddit = Library.Memebot.CreateReddit();

            int memesPerSub = 3;
            Library.Memebot.CollectTopMemesToQueue(memesPerSub, subredditList, reddit, storageConnectionString);

            int totalMemes = memesPerSub * subredditList.Length;
            log.LogInformation($"Collected {totalMemes} memes and stored into queue at {DateTime.Now}");
        }
    }
}
