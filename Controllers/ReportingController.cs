using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportingService.Data;
using ReportingService.Models;
using System.Globalization;
using System.Text;
using NLog;
using NLog.Web;

namespace ReportingService.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportingController : ControllerBase
    {
        private readonly ReportingDbContext _context;

        public ReportingController(ReportingDbContext context)
        {
            _context = context;
        }

        [HttpGet("user-summary")]
        public async Task<IActionResult> GetUserSummary()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Address) // Ensure Address is included
                    .ToListAsync();

                return Ok(users);
            } catch (Exception ex) 
            {
               NLog.LogManager.GetCurrentClassLogger().Error(ex, "Error fetching user summary");
                return Problem("An Error occurred while fetching user summary");
            }
        }


        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts()
        {
            try
            {
                var products = await _context.Products
                    .OrderByDescending(p => p.OrdersReceived)
                    .ToListAsync();
                return Ok(products);
            } catch (Exception ex) 
            {
               NLog.LogManager.GetCurrentClassLogger().Error(ex, "Error fetching top products");
                return Problem("An Error occurred while fetching top products");
    }
}

        [HttpGet("export-csv")]
        public async Task<IActionResult> ExportReportAsCsv([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
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

                var orders = await ordersQuery.ToListAsync();

                var csvBuilder = new StringBuilder();
                csvBuilder.AppendLine("OrderId,UserId,UserName,ProductId,ProductDescription,Price,QuantityOrdered,PurchaseDate,Region");

                foreach (var order in orders)
                {
                    csvBuilder.AppendLine($"{order.OrderId},{order.User.UserId},{order.User.UserName},{order.Product.ProductId},{order.Product.Description},{order.Product.Price},{order.QtyOrdered},{order.PurchaseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)},{order.User.Address.City}");
                }

                var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
                return File(csvBytes, "text/csv", "report.csv");
            }catch (Exception ex) 
            {
               NLog.LogManager.GetCurrentClassLogger().Error(ex, "Error exporting csv report");
                return Problem("An Error occurred while exporting csv report");
    }
}

    }
}
