using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Logging;

namespace SendEmailToReaders
{
    public static class StoreEmail
    {
        [FunctionName("StoreEmail")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, ILogger log)
        {

            var postData = await req.Content.ReadAsFormDataAsync();
            var missingFields = new List<string>();

            if (postData["fromEmail"] == null)
            {
                missingFields.Add("fromEmail");
            }

            if (missingFields.Any())
            {
                var missingFieldsSummary = String.Join(", ", missingFields);
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Missing field(s): {missingFieldsSummary}");
            }

            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));

                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                CloudTable table = tableClient.GetTableReference("Subscriptions");

                await table.CreateIfNotExistsAsync();

                CreateMessage(table, new EmailEntity(postData["fromEmail"]));

                return req.CreateResponse(HttpStatusCode.OK, "Thanks! You've successfully signed up. "); //
            }
            catch (Exception ex)
            {
                return req.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    status = false,
                    message = $"There are problems storing your email address: {ex.GetType()}"
                });
            }
        }

        static void CreateMessage(CloudTable table, EmailEntity newemail)
        {
            TableOperation insert = TableOperation.Insert(newemail);

            table.ExecuteAsync(insert);
        }
    }  
}
