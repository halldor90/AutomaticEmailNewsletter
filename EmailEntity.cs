using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace SendEmailToReaders
{
    class EmailEntity : TableEntity
    {
        public string EmailAddress { get; set; }

        public EmailEntity(string email)
        {
            EmailAddress = email;
            PartitionKey = "SendEmailToReaders";
            RowKey = Guid.NewGuid().ToString();
        }

        public EmailEntity()
        {

        }
    }    
}