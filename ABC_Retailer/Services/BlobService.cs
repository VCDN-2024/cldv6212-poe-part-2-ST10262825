
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ABC_Retailer.Services
{
    public class BlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "store";

        public BlobService(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            try
            {
                // Check if the blob exists
                if (await blobClient.ExistsAsync())
                {
                    throw new InvalidOperationException("A blob with the same name already exists. Please choose a different name.");
                }

                // Upload the blob
                await blobClient.UploadAsync(fileStream, overwrite: true);
                return blobClient.Uri.ToString();
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == "BlobAlreadyExists")
            {
                // Handle the specific case where the blob already exists
                throw new InvalidOperationException("A blob with the same name already exists. Please choose a different name.", ex);
            }
            catch (Exception ex)
            {
                // Handle other potential exceptions
                throw new InvalidOperationException("A blob with the same name or image already exists. Please choose a different name", ex);
            }
        }


        public async Task DeleteBlobAsync(string blobUri)
        {
            Uri uri = new Uri(blobUri);
            string blobName = uri.Segments[^1];
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            try
            {
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
            }
            catch (Exception ex)
            {
                // Handle deletion errors
                throw new InvalidOperationException("An error occurred while deleting the blob.", ex);
            }
        }
    }
}


