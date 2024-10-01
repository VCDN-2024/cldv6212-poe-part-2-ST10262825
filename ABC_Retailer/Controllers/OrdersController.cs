using ABC_Retailer.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ABC_Retailer.Services;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[Authorize]
public class OrdersController : Controller
{
    private readonly TableStorageService _tableStorageService;
    private readonly QueueService _queueService;
    private readonly HttpClient _httpClient;

    public OrdersController(TableStorageService tableStorageService, QueueService queueService, HttpClient httpClient)
    {
        _tableStorageService = tableStorageService;
        _queueService = queueService;
        _httpClient = httpClient;
    }

    // Action to display all orders
    public async Task<IActionResult> Index()
    {
        // Check if the user has an email ending in '.admin'
        if (!IsAdminUser())
        {
            return RedirectToAction("AccessDenied", "Account");
        }
        var orders = await _tableStorageService.GetAllOrdersAsync();
        return View(orders);
    }

    // Action to show the Register Order form
    public async Task<IActionResult> Register()
    {
        // Check if the user has an email ending in '.admin'
        if (!IsAdminUser())
        {
            return RedirectToAction("AccessDenied", "Account");
        }
        var customers = await _tableStorageService.GetAllCustomersAsync();
        var products = await _tableStorageService.GetAllProductsAsync();

        if (customers == null || customers.Count == 0)
        {
            ModelState.AddModelError("", "No customers found. Please add customers first.");
            return View();
        }

        if (products == null || products.Count == 0)
        {
            ModelState.AddModelError("", "No products found. Please add products first.");
            return View();
        }

        ViewData["Customers"] = customers;
        ViewData["Products"] = products;

        return View();
    }

    // Action to handle the form submission and register the order
    [HttpPost]
    public async Task<IActionResult> Register(Order order)
    {
        // Check if the user has an email ending in '.admin'
        if (!IsAdminUser())
        {
            return RedirectToAction("AccessDenied", "Account");
        }
        var existingOrder = await _tableStorageService.GetOrderAsync("OrdersPartition", order.Order_Id.ToString());
        if (existingOrder != null)
        {
            ModelState.AddModelError("Order_Id", "An order with this ID already exists.");
        }

        if (ModelState.IsValid)
        {
            order.Order_Date = DateTime.SpecifyKind(order.Order_Date, DateTimeKind.Utc);
            order.PartitionKey = "OrdersPartition";
            order.RowKey = order.Order_Id.ToString();

            await _tableStorageService.AddOrderAsync(order);

            string message = $"New order by Customer {order.Customer_Id} for Product {order.Product_Id} at {order.Order_Address} on {order.Order_Date}";
            await _queueService.SendMessageAsync(message);

            return RedirectToAction("Index");
        }

        var customers = await _tableStorageService.GetAllCustomersAsync();
        var products = await _tableStorageService.GetAllProductsAsync();
        ViewData["Customers"] = customers;
        ViewData["Products"] = products;

        return View(order);
    }

    // Action to display the Edit Order form
    public async Task<IActionResult> Edit(string partitionKey, string rowKey)
    {
        var order = await _tableStorageService.GetOrderAsync(partitionKey, rowKey);
        if (order == null)
        {
            return NotFound();
        }

        var customers = await _tableStorageService.GetAllCustomersAsync();
        var products = await _tableStorageService.GetAllProductsAsync();
        ViewData["Customers"] = customers;
        ViewData["Products"] = products;

        return View(order);
    }

    // Action to handle the form submission for editing an order
    [HttpPost]
    public async Task<IActionResult> Edit(Order order)
    {
        if (ModelState.IsValid)
        {
            order.Order_Date = DateTime.SpecifyKind(order.Order_Date, DateTimeKind.Utc);
            await _tableStorageService.AddOrderAsync(order); // Add logic to update the order
            return RedirectToAction("Index");
        }

        var customers = await _tableStorageService.GetAllCustomersAsync();
        var products = await _tableStorageService.GetAllProductsAsync();
        ViewData["Customers"] = customers;
        ViewData["Products"] = products;

        return View(order);
    }

    // Action to delete an order
    [HttpPost]
    public async Task<IActionResult> DeleteOrder(string partitionKey, string rowKey)
    {
        await _tableStorageService.DeleteOrderAsync(partitionKey, rowKey);
        return RedirectToAction("Index");
    }

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

    public async Task<IActionResult> BuyProducts()
    {
        // Retrieve all products from Table Storage
        var products = await _tableStorageService.GetAllProductsAsync();
        return View(products); // Return the view with the product list
    }


    // Action to show the Register Order form
    public async Task<IActionResult> PlaceOrder()
    {
        var customers = await _tableStorageService.GetAllCustomersAsync();
        var products = await _tableStorageService.GetAllProductsAsync();

        if (customers == null || customers.Count == 0)
        {
            ModelState.AddModelError("", "No customers found. Please add customers first.");
            return View();
        }

        if (products == null || products.Count == 0)
        {
            ModelState.AddModelError("", "No products found. Please add products first.");
            return View();
        }

        ViewData["Customers"] = customers;
        ViewData["Products"] = products;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(Order order)
    {
        var existingOrder = await _tableStorageService.GetOrderAsync("OrdersPartition", order.Order_Id.ToString());
        if (existingOrder != null)
        {
            ModelState.AddModelError("Order_Id", "An order with this ID already exists.");
        }

        if (ModelState.IsValid)
        {
            order.Order_Date = DateTime.SpecifyKind(order.Order_Date, DateTimeKind.Utc);
            order.PartitionKey = "OrdersPartition";
            order.RowKey = order.Order_Id.ToString();

            await _tableStorageService.AddOrderAsync(order);

            string message = $"New order by Customer {order.Customer_Id} for Product {order.Product_Id} at {order.Order_Address} on {order.Order_Date}";
            await _queueService.SendMessageAsync(message);

            return RedirectToAction("Index");
        }

        var customers = await _tableStorageService.GetAllCustomersAsync();
        var products = await _tableStorageService.GetAllProductsAsync();
        ViewData["Customers"] = customers;
        ViewData["Products"] = products;

        return View(order);
    }

    public IActionResult OrderSuccess()
    {
        return View();
    }

  
    public async Task<IActionResult> OrderConfirm()
    {

        return View(); // Change this to your success view name
    }


    // Helper method to check if the user's email ends in '.admin'
    private bool IsAdminUser()
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        return !string.IsNullOrEmpty(userEmail) && userEmail.EndsWith(".admin");
    }
}
