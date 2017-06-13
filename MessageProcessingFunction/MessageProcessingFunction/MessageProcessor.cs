using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace MessageProcessingFunction
{
    public static class MessageProcessor
    {
        [FunctionName("EventHubTriggerCSharp")]
        public static void Run([EventHubTrigger("functioneventhub", Connection = "EventHubConnectionString")]List<Message> myEventHubMessage, [Blob("companysetups/companyGuid.json", access: FileAccess.ReadWrite, Connection = "StorageAccountConnectionString")] ICloudBlob myInputBlob, TraceWriter log)
        {
            // get blob data, input blob binding not working: https://github.com/Azure/azure-functions-vs-build-sdk/issues/52 
            CloudBlob cloudBlob = blobContainer.GetBlobReference(myEventHubMessage.companyGuid+".json");
            Stream stream = new MemoryStream();
            myInputBlob.DownloadToStream(stream);
            stream.Position = 0;
            var sr = new StreamReader(stream);
            string json = sr.ReadToEnd();

            LocalEntities.Company = JsonConvert.DeserializeObject<Company>(json);

            log.Info("Processing message");
            Core.MessageProcessor.ProcessMesssage(myEventHubMessage);
            log.Info($"C# Event Hub trigger function processed a message: {myEventHubMessage}");
        }
    }
}