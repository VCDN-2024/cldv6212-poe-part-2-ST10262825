using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace ABC_Retailer.Models
{
    public class Customer : ITableEntity
    {
        [Key]
        [Range(100, 999, ErrorMessage = "Customer ID must be a three-digit number.")]
        public int Customer_Id { get; set; }  // Ensure this property exists and is populated
        public string? Customer_Name { get; set; }  // Ensure this property exists and is populated
        public string? Customer_Surname { get; set; }
        public string? email { get; set; }
        public string? phoneNumber { get; set; }   

        // ITableEntity implementation
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
