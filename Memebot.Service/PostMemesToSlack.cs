using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Memebot.Library;

namespace Memebot.PostMemes
{
    public static class PostMemesToSlack
    {
        [FunctionName("PostMemesToSlack")]
        public static void Run([TimerTrigger("0 0 */2 * * *")]TimerInfo myTimer, ILogger log)
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
}
