using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

public class WriteToBlobFunction
{
    private readonly ILogger<WriteToBlobFunction> _logger;

    public WriteToBlobFunction(ILogger<WriteToBlobFunction> logger)
    {
        _logger = logger;
    }

    [Function("WriteToBlobFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Processing a request to upload a product image to Azure Blob Storage.");

        try
        {
            // Check if the request contains multipart/form-data
            if (!req.Headers.TryGetValues("Content-Type", out var contentType) ||
                !contentType.ToString().StartsWith("multipart/form-data"))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request does not contain multipart/form-data.");
                return badResponse;
            }

            // Manually extract boundary from the Content-Type header
            var boundary = MultipartRequestHelper.GetBoundary(contentType.ToString(), MultipartRequestHelper.DefaultMultipartBoundary).ToString();

            var reader = new MultipartReader(boundary, req.Body);

            var section = await reader.ReadNextSectionAsync();
            if (section == null)
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("No file found in the request.");
                return badResponse;
            }

            // Ensure we are reading the file part
            if (section.ContentDisposition.Contains("filename"))
            {
                // Safely extract the file name
                var fileName = ExtractFileName(section.ContentDisposition);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    var badResponseNoFileName = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponseNoFileName.WriteStringAsync("File name could not be extracted.");
                    return badResponseNoFileName;
                }

                var blobName = $"{Guid.NewGuid()}_{fileName}"; // Generate a unique name

                // Get connection string from environment variables
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                if (string.IsNullOrEmpty(connectionString))
                {
                    var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                    await errorResponse.WriteStringAsync("Azure Storage connection string is missing.");
                    return errorResponse;
                }

                // Create Blob Service Client and Container Client
                string containerName = "product-images"; // Update this if necessary
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                // Ensure the container exists
                await containerClient.CreateIfNotExistsAsync();

                // Upload the file to Blob Storage
                var blobClient = containerClient.GetBlobClient(blobName);
                using (var stream = section.Body)
                {
                    await blobClient.UploadAsync(stream, new Azure.Storage.Blobs.Models.BlobHttpHeaders
                    {
                        ContentType = section.ContentType // Set the content type
                    });
                }

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync($"Product image uploaded successfully: {blobClient.Uri}");
                return response;
            }

            // If no valid file section found
            var badResponseNoFileSection = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badResponseNoFileSection.WriteStringAsync("No valid file section found in the request.");
            return badResponseNoFileSection;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error uploading product image: {ex.Message}");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("An error occurred while uploading the product image.");
            return errorResponse;
        }
    }

    private string ExtractFileName(string contentDisposition)
    {
        // Split by ';' to get parameters
        var elements = contentDisposition.Split(';');
        foreach (var element in elements)
        {
            // Check for filename parameter
            if (element.Trim().StartsWith("filename=", StringComparison.OrdinalIgnoreCase))
            {
                // Remove quotes and return the file name
                return element.Substring("filename=".Length).Trim().Trim('\"');
            }
        }
        return null; // Return null if not found
    }
}

public static class MultipartRequestHelper
{
    public static readonly string DefaultMultipartBoundary = "------------------------Boundary";

    public static string GetBoundary(string contentType, string defaultBoundary)
    {
        var elements = contentType.Split(';');
        foreach (var element in elements)
        {
            if (element.Trim().StartsWith("boundary=", StringComparison.OrdinalIgnoreCase))
            {
                return element.Substring("boundary=".Length).Trim().Trim('\"');
            }
        }
        return defaultBoundary;
    }
}
