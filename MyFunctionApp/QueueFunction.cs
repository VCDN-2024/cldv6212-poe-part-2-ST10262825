using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Newtonsoft.Json;

namespace MyFunctionApp
{
    public static class QueueFunction
    {
        [Function("QueueFunction")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("QueueFunction");
            logger.LogInformation("Queue function triggered.");

            // Get the connection string from environment
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string queueName = "transaction-queue"; // Queue name

            QueueClient queueClient = new QueueClient(connectionString, queueName);

            // Ensure the queue is created if it doesn't exist
            await queueClient.CreateIfNotExistsAsync();

            // Check if the request method is POST
            if (req.Method == "POST")
            {
                // Read the request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                if (string.IsNullOrEmpty(requestBody))
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Message content is empty.");
                    return badRequestResponse;
                }

                // Optionally: Deserialize the product message if needed
                var productMessage = JsonConvert.DeserializeObject<Product>(requestBody);
                if (productMessage == null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Invalid product data.");
                    return badRequestResponse;
                }

                // Send the message to the queue
                await queueClient.SendMessageAsync(requestBody);

                var postResponse = req.CreateResponse(HttpStatusCode.OK);
                await postResponse.WriteStringAsync("Message written to the queue successfully.");
                return postResponse;
            }

            var methodNotAllowedResponse = req.CreateResponse(HttpStatusCode.MethodNotAllowed);
            await methodNotAllowedResponse.WriteStringAsync("Invalid HTTP method. Use POST to write to the queue.");
            return methodNotAllowedResponse;
        }
    }
}
