using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ABC_Retailer.Services;

var builder = WebApplication.CreateBuilder(args);

// Access the configuration object
var configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();

// Register HttpClient
builder.Services.AddHttpClient();

// Register BlobService with configuration
builder.Services.AddSingleton<BlobService>(sp =>
    new BlobService(configuration.GetConnectionString("AzureStorage")));

// Register TableStorageService with configuration
builder.Services.AddSingleton<TableStorageService>(sp =>
    new TableStorageService(configuration.GetConnectionString("AzureStorage")));

// Register QueueService with configuration
builder.Services.AddSingleton<QueueService>(sp =>
{
    var connectionString = configuration.GetConnectionString("AzureStorage");
    return new QueueService(connectionString, "order-queue");
});

// Register FileShareService with configuration
builder.Services.AddSingleton<AzureFileShareService>(sp =>
{
    var connectionString = configuration.GetConnectionString("AzureStorage");
    return new AzureFileShareService(connectionString, "contracts-logs");
});

// Register AzureFunctionsService
builder.Services.AddSingleton<AzureFunctionsService>();

// Register authentication and authorization services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

// Register UserRepository and AuthService for authentication
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
