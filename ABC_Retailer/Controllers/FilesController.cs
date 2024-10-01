using ABC_Retailer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

[Authorize]
public class FilesController : Controller
{
    private readonly AzureFileShareService _fileShareService;
    private readonly HttpClient _httpClient;
    private readonly string _functionBaseUrl = "https://retailerfunction.azurewebsites.net/api/FileUploadFunction?code=0MW3VOp9Kt0pWi3g8Ajr446Iwlmb0x7SGGDvPM_gf9jWAzFuxkCGaQ%3D%3D";  // URL for the file upload function

    public FilesController(AzureFileShareService fileShareService, HttpClient httpClient)
    {
        _fileShareService = fileShareService;
        _httpClient = httpClient;
    }

    // GET: Files/Index
    public async Task<IActionResult> Index()
    {
       

        List<FileModel> files;
        try
        {
            files = await _fileShareService.ListFilesAsync("uploads");
        }
        catch (Exception ex)
        {
            ViewBag.Message = $"Failed to load files: {ex.Message}";
            files = new List<FileModel>();
        }

        return View(files);
    }

    // POST: Files/Upload
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
       

        if (file == null || file.Length == 0)
        {
            TempData["Message"] = "Please select a file to upload.";
            return RedirectToAction("Index");
        }

        try
        {
            using (var stream = file.OpenReadStream())
            {
                // Step 1: Upload file to Azure File Share via Function (without multipart form data)
                await UploadFileToFunction(file.FileName, stream);
            }

            TempData["Message"] = $"File '{file.FileName}' uploaded successfully!";
        }
        catch (Exception ex)
        {
            TempData["Message"] = $"File upload failed: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    // GET: Files/DownloadFile
    [HttpGet]
    public async Task<IActionResult> DownloadFile(string fileName)
    {
      
       

        // Logic for downloading a file
        // You can integrate your download logic here using _fileShareService or HttpClient
        return View();
    }

    // POST: Files/DeleteFile
    public async Task<IActionResult> DeleteFile(string fileName)
    {
       

        try
        {
            // Assuming _fileShareService.DeleteFileAsync is a method to delete the file from Azure File Share
            await _fileShareService.DeleteFileAsync("uploads", fileName);
            TempData["Message"] = $"File '{fileName}' deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Message"] = $"Failed to delete file: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    // Helper method to send the file stream directly
    private async Task UploadFileToFunction(string fileName, Stream stream)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _functionBaseUrl)
        {
            Content = new StreamContent(stream)
        };

        // Add file name as a header (instead of using multipart form-data)
        request.Headers.Add("file-name", fileName);

        // Log for debugging purposes
        Console.WriteLine("Sending file upload request with file-name: " + fileName);

        // Send the request
        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Error uploading file: {errorContent}");
        }
    }

    // Helper method to check if the user's email ends in '.admin'
    private bool IsAdminUser()
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        return !string.IsNullOrEmpty(userEmail) && userEmail.EndsWith(".admin");
    }
}
