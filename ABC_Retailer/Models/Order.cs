using System;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retailer.Models
{
    public class Order : ITableEntity
    {
        [Key]
        [Range(100, 999, ErrorMessage = "Order ID must be a three-digit number.")]
        public int Order_Id { get; set; }

        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        //Introduce validation sample
        [Required(ErrorMessage = "Please select a customer.")]
        public int Customer_Id { get; set; } // FK to the Birder who made the sighting

        [Required(ErrorMessage = "Please select a product.")]
        public int Product_Id { get; set; } // FK to the Bird being sighted

        [Required(ErrorMessage = "Please select the date.")]
        public DateTime Order_Date { get; set; } 

        [Required(ErrorMessage = "Please enter the location.")]
        public string? Order_Address { get; set; } 
    }
}
