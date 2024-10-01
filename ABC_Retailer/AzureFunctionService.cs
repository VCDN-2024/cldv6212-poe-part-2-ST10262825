using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using ABC_Retailer.Models;

public class AzureFunctionsService
{
    private readonly HttpClient _httpClient;
    private const string FunctionKey = "4a0Edlsc1qBBJ9BmUZgOSf7BFdaZmUjELZ0cKA4PFoiIAzFuiAQhzg=="; // Your function key

    public AzureFunctionsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> StoreTableDataAsync(object data)
    {
        var requestUri = $"https://retailerfunctions2.azurewebsites.net/"; // Updated with function key
        var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> UploadBlobAsync(IFormFile file)
    {
        var requestUri = $"https://retailerfunctions2.azurewebsites.net/"; // Updated with function key
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(file.OpenReadStream()), "file", file.FileName);

        var response = await _httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> AddQueueMessageAsync(object message)
    {
        var requestUri = $"https://retailerfunctions2.azurewebsites.net/"; // Updated with function key
        var content = new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> UploadToFileShareAsync(IFormFile file)
    {
        var requestUri = $"https://retailerfunction.azurewebsites.net/"; // Updated with function key
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(file.OpenReadStream()), "file", file.FileName);

        var response = await _httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    public async Task DeleteBlobAsync(string blobUrl)
    {
        var requestUri = $"https://retailerfunctions2.azurewebsites.net/api/DeleteBlob?code={FunctionKey}"; // Updated with function key
        var content = new StringContent(JsonConvert.SerializeObject(new { BlobUrl = blobUrl }), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();
    }

  

}
