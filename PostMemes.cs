using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Memebot.PostMemes
{
    public static class PostMemes
    {
        [FunctionName("PostMemes")]
        public static void Run([TimerTrigger("0 0 21 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            log.LogInformation("test ree");
        }
    }
}
