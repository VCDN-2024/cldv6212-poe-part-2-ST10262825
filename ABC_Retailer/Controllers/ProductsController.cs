using ABC_Retailer.Models;
using ABC_Retailer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class ProductsController : Controller
{
    private readonly BlobService _blobService;
    private readonly TableStorageService _tableStorageService;
    private readonly QueueService _queueService;
    private readonly HttpClient _httpClient;

    public ProductsController(BlobService blobService, TableStorageService tableStorageService, QueueService queueService, HttpClient httpClient)
    {
        _blobService = blobService;
        _tableStorageService = tableStorageService;
        _queueService = queueService;
        _httpClient = httpClient;
    }

    [HttpGet]
    public IActionResult AddProduct()
    {
        return View();
    }

    [HttpPost]
    [HttpPost]
    public async Task<IActionResult> AddProduct(Product product, IFormFile file)
    {
        // Check if the user has an email ending in '.admin'
        if (!IsAdminUser())
        {
            return RedirectToAction("AccessDenied", "Account");
        }
        try
        {
            if (file != null)
            {
                using var stream = file.OpenReadStream();
                var imageUrl = await _blobService.UploadAsync(stream, file.FileName);
                product.ImageUrl = imageUrl;  // Set the Image URL
            }

            if (ModelState.IsValid)
            {
                // Set PartitionKey and RowKey
                product.PartitionKey = "ProductsPartition";
                product.RowKey = Guid.NewGuid().ToString(); // Generate a unique RowKey

                // Convert product to JSON and send it to the Azure Function
                string requestUri = $"https://retailerfunction.azurewebsites.net/api/StoreToTable?code=0MW3VOp9Kt0pWi3g8Ajr446Iwlmb0x7SGGDvPM_gf9jWAzFuxkCGaQ%3D%3D"; // Your Azure Function URL with code
                var json = JsonConvert.SerializeObject(product);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(requestUri, content); // Call the Azure Function

                if (!response.IsSuccessStatusCode)
                {
                    // Handle unsuccessful response from Azure Function
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Error storing product: {errorContent}");
                    return View(product);
                }

                // Prepare the message for the queue
                var queueMessage = JsonConvert.SerializeObject(new
                {
                    Message = $"Successfully added: {product.Product_Name} at R{product.Price}. Image URL: {product.ImageUrl}",
                    Product_Name = product.Product_Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl
                });

                // Send message to the Queue Function
                string queueFunctionUri = "https://retailerfunction.azurewebsites.net/api/QueueFunction?code=0MW3VOp9Kt0pWi3g8Ajr446Iwlmb0x7SGGDvPM_gf9jWAzFuxkCGaQ%3D%3D"; // Replace with your Queue Function URL
                var queueContent = new StringContent(queueMessage, Encoding.UTF8, "application/json");

                var queueResponse = await _httpClient.PostAsync(queueFunctionUri, queueContent);

                if (!queueResponse.IsSuccessStatusCode)
                {
                    // Handle unsuccessful response from Queue Function
                    var errorContent = await queueResponse.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Error sending message to queue: {errorContent}");
                    return View(product);
                }

                // Redirect to Index page with a success message
                TempData["SuccessMessage"] = $"Product '{product.Product_Name}' has been added successfully.";
                return RedirectToAction("Index");
            }

            // Return the view with the model if there are validation issues
            return View(product);
        }
        catch (Exception ex)
        {
            // Handle unexpected errors
            ModelState.AddModelError("", "An unexpected error occurred: " + ex.Message);
            return View(product);
        }
    }


    [HttpPost]
    public async Task<IActionResult> DeleteProduct(string partitionKey, string rowKey, Product product)
    {
        try
        {
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                // Delete the associated image from Blob storage
                await _blobService.DeleteBlobAsync(product.ImageUrl);
            }

            // Delete the product from Table Storage
            await _tableStorageService.DeleteProductAsync(partitionKey, rowKey);

            // Redirect to the Index page after successful deletion
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            // Handle deletion errors
            ModelState.AddModelError("", $"Error deleting product: {ex.Message}");
            return View(product);
        }
    }

    public async Task<IActionResult> Index()
    {
        // Check if the user has an email ending in '.admin'
        if (!IsAdminUser())
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Retrieve all products from Table Storage
        var products = await _tableStorageService.GetAllProductsAsync();
        return View(products);
    }

    public async Task<IActionResult> BuyProducts()
    {
        // Retrieve all products from Table Storage
        var products = await _tableStorageService.GetAllProductsAsync();
        return View(products); // Return the view with the product list
    }

    private bool IsAdminUser()
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        return !string.IsNullOrEmpty(userEmail) && userEmail.EndsWith(".admin");
    }

}




