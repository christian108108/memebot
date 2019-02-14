using Memebot.Library;

namespace Memebot.Sandbox
{
    class Program
    {
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

            // log into KeyVault and get secrets
            KeyVaultHelper.LogIntoKeyVault();
            var storageConnectionString = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/storage-connection-string/");
            var slackWebhookUrl = KeyVaultHelper.GetSecret("https://memebot-keyvault.vault.azure.net/secrets/slack-webhook-url/");


            // collect memes and store in Azure Queue Storage
            var reddit = Library.Memebot.CreateReddit();
            Library.Memebot.CollectTopMemesToQueue(4, subredditList, reddit, storageConnectionString);


            // post memes from Azure Queue Storage into Slack
            Library.Memebot.PostToSlackFromQueue(2, slackWebhookUrl, storageConnectionString);

            ;
        }
    }
}