using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ReportingService.Services;
using System;
using NLog;

namespace ReportingService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportingController : ControllerBase
    {
        private readonly IReportingService _reportingService;

        public ReportingController(IReportingService reportingService)
        {
            _reportingService = reportingService;
        }

        [HttpGet("user-summary")]
        public async Task<IActionResult> GetUserSummary()
        {
            try
            {
                var users = await _reportingService.GetUserSummaryAsync();
                return Ok(users);
            }
            catch (UnauthorizedAccessException) // Handle unauthorized access
            {
                return Unauthorized(new { message = "Invalid or expired token" });
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "Error fetching user summary");
                return StatusCode(500, new { message = "An error occurred while fetching user summary.." });
            }
        }

        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts()
        {
            try
            {
                var products = await _reportingService.GetTopProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "Error fetching top products");
                return StatusCode(500, new { message = "An error occurred while fetching top products" });
            }
        }

        [HttpGet("export-csv")]
        public async Task<IActionResult> ExportReportAsCsv([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                // Validate date range
                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    return BadRequest(new { message = "Start date cannot be greater than end date" });
                }
                var fileResult = await _reportingService.ExportOrdersAsCsvAsync(startDate, endDate);
                return File(fileResult.Content, fileResult.ContentType, fileResult.FileName);
            }
            catch (ArgumentException ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "Invalid input parameters for CSV export");
                return BadRequest(new { message = "Invalid input parameters for CSV export" });
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "Error exporting CSV report");
                return StatusCode(500, new { message = "An error occurred while exporting CSV report" });
            }
        }
    }
}
