using System.IO;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using Microsoft.Azure.Functions.Worker.Http;
using Azure;
using System.ComponentModel.DataAnnotations;

namespace MyFunctionApp
{
    public class StoreToTableFunction
    {
        private readonly ILogger<StoreToTableFunction> _logger;

        public StoreToTableFunction(ILogger<StoreToTableFunction> logger)
        {
            _logger = logger;
        }

        [Function("StoreToTable")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req)
        {
            _logger.LogInformation("Processing a request to store data into Azure Table Storage.");

            try
            {
                // Read and deserialize the request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var product = JsonConvert.DeserializeObject<Product>(requestBody);

                // Validate the product data
                if (product == null || product.Product_Id < 100 || product.Product_Id > 999 || string.IsNullOrEmpty(product.Product_Name))
                {
                    _logger.LogWarning("Invalid product data received.");
                    var badRequestResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Invalid product data. Ensure the product ID is a three-digit number and product name is provided.");
                    return badRequestResponse;
                }

                // Get the connection string from the environment variable
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("Azure Storage connection string is not set.");
                    var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                    await errorResponse.WriteStringAsync("Azure Storage connection string is not set.");
                    return errorResponse;
                }

                // Create a TableClient and ensure the table exists
                var tableClient = new TableClient(connectionString, "products");
                await tableClient.CreateIfNotExistsAsync();

                // Create a new product entity and populate it with product data
                var entity = new TableEntity(product.Category, product.Product_Id.ToString())
                {
                    { "Product_Name", product.Product_Name },
                    { "Description", product.Description },
                    { "Price", product.Price },
                      { "Category", product.Category },
                    { "ImageUrl", product.ImageUrl }
                };

                // Add the entity to the table
                await tableClient.AddEntityAsync(entity);
                _logger.LogInformation($"Product {product.Product_Name} added successfully to Azure Table Storage.");

                var successResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await successResponse.WriteStringAsync("Product stored successfully.");
                return successResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error storing product data: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error storing product data: {ex.Message}");
                return errorResponse;
            }
        }
    }

    // Product model based on ABC_Retailer.Models.Product
    public class Product : ITableEntity
    {
        [Key]
        [Range(100, 999, ErrorMessage = "Product ID must be a three-digit number.")]
        public int Product_Id { get; set; }  // Ensure this property exists and is populated
        public string? Product_Name { get; set; }  // Ensure this property exists and is populated
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? Price { get; set; }
        public string Category { get; set; }

        // ITableEntity implementation
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
