using Microsoft.EntityFrameworkCore;
using ReportingService.Data;
using ReportingService.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportingService.Services
{
    public class ReportingServiceImpl : IReportingService
    {
        private readonly ReportingDbContext _context;

        public ReportingServiceImpl(ReportingDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetUserSummaryAsync()
        {
            return await _context.Users
                .Include(u => u.Address) // Ensure Address is included
                .Include(u => u.Orders)
                .ToListAsync();
        }

        public async Task<List<Product>> GetTopProductsAsync()
        {
            return await _context.Products
                .OrderByDescending(p => p.OrdersReceived)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersAsync(DateTime? startDate, DateTime? endDate)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Product)
                .Include(o => o.User.Address)
                .AsQueryable();

            // Apply date range filter if provided
            if (startDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.PurchaseDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.PurchaseDate <= endDate.Value);
            }

            return await ordersQuery.ToListAsync();
        }

        public class FileResultModel
        {
            public byte[] Content { get; set; }
            public string ContentType { get; set; }
            public string FileName { get; set; }
        }

        public async Task<FileResultModel> ExportOrdersAsCsvAsync(DateTime? startDate, DateTime? endDate)
        {
            var orders = await GetOrdersAsync(startDate, endDate);

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("OrderId,UserId,UserName,ProductId,ProductDescription,Price,QuantityOrdered,PurchaseDate,Region");

            foreach (var order in orders)
            {
                csvBuilder.AppendLine($"{order.OrderId},{order.User.UserId},{order.User.UserName},{order.Product.ProductId},{order.Product.Description},{order.Product.Price},{order.QtyOrdered},{order.PurchaseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)},{order.User.Address.City}");
            }

            return new FileResultModel
            {
                Content = Encoding.UTF8.GetBytes(csvBuilder.ToString()),
                ContentType = "text/csv",
                FileName = $"Order_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv"
            };
        }

    }
}
