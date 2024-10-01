using ABC_Retailer.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ABC_Retailer.Services;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[Authorize]
public class CustomersController : Controller
{
    private readonly TableStorageService _tableStorageService;
    private readonly QueueService _queueService;
    private readonly HttpClient _httpClient;

    public CustomersController(TableStorageService tableStorageService, QueueService queueService, HttpClient httpClient)
    {
        _tableStorageService = tableStorageService;
        _queueService = queueService;
        _httpClient = httpClient;
    }

    public async Task<IActionResult> Index()
    {
        // Check if the user has an email ending in '.admin'
        if (!IsAdminUser())
        {
            return RedirectToAction("AccessDenied", "Account");
        }
        var customers = await _tableStorageService.GetAllCustomersAsync();
        return View(customers);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Customer customer)
    {
        try
        {
            // Check if the Customer_Id already exists
            var existingCustomer = await _tableStorageService.GetCustomerAsync("CustomersPartition", customer.Customer_Id.ToString());
            if (existingCustomer != null)
            {
                ModelState.AddModelError("Customer_Id", "A customer with this ID already exists.");
                return View(customer);
            }

            // Set PartitionKey and RowKey for Table Storage
            customer.PartitionKey = "CustomersPartition";
            customer.RowKey = customer.Customer_Id.ToString(); // Use Customer_Id as RowKey

            // Add the customer to Table Storage
            await _tableStorageService.AddCustomerAsync(customer);
            string message = $"New Customer Added with name {customer.Customer_Name}, email: {customer.email} and Phone Number  {customer.phoneNumber} ";
            await _queueService.SendMessageAsync(message);

            return RedirectToAction("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(customer);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An unexpected error occurred.");
            return View(customer);
        }
    }


    public async Task<IActionResult> Delete(string partitionKey, string rowKey)
    {
        await _tableStorageService.DeleteCustomerAsync(partitionKey, rowKey);
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Details(string partitionKey, string rowKey)
    {
        var customer = await _tableStorageService.GetCustomerAsync(partitionKey, rowKey);
        if (customer == null)
        {
            return NotFound();
        }
        return View(customer);
    }

    // Example of using HttpClient in this controller
    public async Task<IActionResult> CallExternalFunction()
    {
        string functionUrl = "https://retailerfunction.azurewebsites.net/";

        var response = await _httpClient.GetAsync(functionUrl);

        if (response.IsSuccessStatusCode)
        {
            string result = await response.Content.ReadAsStringAsync();
            return Ok(result);
        }
        return StatusCode((int)response.StatusCode, "Failed to call function");
    }

    private bool IsAdminUser()
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        return !string.IsNullOrEmpty(userEmail) && userEmail.EndsWith(".admin");
    }


}
