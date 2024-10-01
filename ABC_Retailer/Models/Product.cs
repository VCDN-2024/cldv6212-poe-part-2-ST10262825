using Azure;
using Azure.Data.Tables;
using System;
using System.ComponentModel.DataAnnotations;

namespace ABC_Retailer.Models
{
    public class Product : ITableEntity
    {
        [Key]
        [Range(100, 999, ErrorMessage = "product ID must be a three-digit number.")]
        public int Product_Id { get; set; }  // Ensure this property exists and is populated
        public string? Product_Name { get; set; }  // Ensure this property exists and is populated
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? Price { get; set; }
        public string Category { get; set; }

        // ITableEntity implementation
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }

}
