using ABC_Retailer.Models;
using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABC_Retailer.Services
{
    public class TableStorageService
    {
        private readonly TableClient _ordersTableClient;
        private readonly TableClient _customersTableClient;
        private readonly TableClient _productsTableClient;

        public TableStorageService(string connectionString)
        {
            var serviceClient = new TableServiceClient(connectionString);

            _ordersTableClient = serviceClient.GetTableClient("Orders");
            _customersTableClient = serviceClient.GetTableClient("Customers");
            _productsTableClient = serviceClient.GetTableClient("Products");
        }

        // Customer Operations
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var customers = new List<Customer>();
            await foreach (Customer customer in _customersTableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }
            return customers;
        }

        public async Task<Customer?> GetCustomerAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _customersTableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            if (string.IsNullOrEmpty(customer.PartitionKey) || string.IsNullOrEmpty(customer.RowKey))
            {
                throw new ArgumentException("PartitionKey and RowKey must be set.");
            }

            try
            {
                await _customersTableClient.AddEntityAsync(customer);
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Error adding customer to Table Storage", ex);
            }
        }

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await _customersTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // Order Operations
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var orders = new List<Order>();
            await foreach (Order order in _ordersTableClient.QueryAsync<Order>())
            {
                orders.Add(order);
            }
            return orders;
        }

        public async Task<Order?> GetOrderAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _ordersTableClient.GetEntityAsync<Order>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task AddOrderAsync(Order order)
        {
            await _ordersTableClient.UpsertEntityAsync(order, TableUpdateMode.Replace);
        }

        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            await _ordersTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // Product Operations (if necessary)
        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();
            await foreach (Product product in _productsTableClient.QueryAsync<Product>())
            {
                products.Add(product);
            }
            return products;
        }

        public async Task<Product?> GetProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _productsTableClient.GetEntityAsync<Product>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task AddProductAsync(Product product)
        {
            if (string.IsNullOrEmpty(product.PartitionKey) || string.IsNullOrEmpty(product.RowKey))
            {
                throw new ArgumentException("PartitionKey and RowKey must be set.");
            }

            try
            {
                await _productsTableClient.AddEntityAsync(product);
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Error adding product to Table Storage", ex);
            }
        }

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            await _productsTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }


    }


}

