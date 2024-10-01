using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

public class User : ITableEntity
{
    [Key]
    public string PasswordHash { get; set; } // Store hashed password
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public User()
    {
        PartitionKey = "User";
    }
} 
