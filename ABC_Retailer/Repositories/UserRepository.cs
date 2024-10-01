using Azure.Data.Tables;
using Azure;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class UserRepository : IUserRepository
{
    private readonly TableClient _tableClient;

    public UserRepository(IConfiguration configuration)
    {
        // Retrieve the connection string from the configuration
        var connectionString = configuration.GetConnectionString("AzureStorage");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString), "AzureStorage connection string is null or empty.");
        }

        // Initialize the TableClient with the connection string and table name
        _tableClient = new TableClient(connectionString, "User"); // Replace "Users" with your table name
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        try
        {
            // Assuming RowKey is the email
            var response = await _tableClient.GetEntityAsync<User>("User", email);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Return null if the user does not exist
            return null;
        }
    }

    public async Task<bool> CreateUserAsync(User user)
    {
        try
        {
            // Add the user to the table
            await _tableClient.AddEntityAsync(user);
            return true;
        }
        catch (RequestFailedException)
        {
            // Handle any errors (e.g., duplicate RowKey)
            return false;
        }
    }
}
