using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Azure.WebJobs;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.ServiceModel.Syndication;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Logging;

namespace SendEmailToReaders
{
    public static class SendEmail
    {
        [FunctionName("SendEmail")]
        public static async Task Run([TimerTrigger("0 0 17  * * *")]TimerInfo myTimer, ILogger log)
        {
            // Get feed 
            string feedurl = "https://www.fotbolti.net/rss.php";
            string lastday = "";
            bool sendStadfest = false;

            XmlReader reader = XmlReader.Create(feedurl);
            SyndicationFeed feed = SyndicationFeed.Load(reader);

            lastday = lastday + "<b>Nýjar (Staðfest) fréttir undanfarin dag:</b><br><br>";
            foreach (SyndicationItem item in feed.Items)
            {
                if ((DateTime.Now - item.PublishDate).TotalDays < 1 && item.Title.Text.Contains("(Staðfest)"))
                {
                    lastday = lastday + "<a href=\"" + item.Links[0].Uri + "\">" + item.Title.Text + "</a><br>";
                    sendStadfest = true;
                }
            }

            reader.Close();

            if (sendStadfest)
            {
                // Get subscription table
                var storageAccountConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("Subscriptions");
                table.CreateIfNotExistsAsync().Wait();

                List<EmailAddress> recipientlist = GetAllEmailAddresses(table);

                var apiKey = System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
                var sendGridClient = new SendGridClient(apiKey);
                var from = new EmailAddress("halldor@basictechgeek.com", "Fotbolti.net (Staðfest)");
                
                var subject = "Daglegt fréttabréf fyrir Fotbolti.net (Staðfest) uppfærslur";
                var displayRecipients = false; // set this to true if you want recipients to see each others mail id 

                var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, recipientlist, subject, "", lastday, displayRecipients);
                var response = await sendGridClient.SendEmailAsync(msg);
            }
        }

        public static List<EmailAddress> GetAllEmailAddresses(CloudTable table)
        {
            
            var retList = new List<EmailAddress>();
            
            TableQuery<EmailEntity> query = new TableQuery<EmailEntity>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "SendEmailToReaders"));

            var segment = table.ExecuteQuerySegmentedAsync(query, null);
            foreach (EmailEntity entity in segment.Result)
            {
                retList.Add(MailHelper.StringToEmailAddress(entity.EmailAddress));
            }

            return retList;
        }
    }
}
