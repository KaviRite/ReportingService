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
            catch (Exception ex)
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
                var products = await _reportingService.GetTopProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
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
                var fileResult = await _reportingService.ExportOrdersAsCsvAsync(startDate, endDate);
                return File(fileResult.Content, fileResult.ContentType, fileResult.FileName);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "Error exporting CSV report");
                return Problem("An Error occurred while exporting CSV report");
            }
        }

    }
}
