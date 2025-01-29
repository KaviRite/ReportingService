using ReportingService.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static ReportingService.Services.ReportingServiceImpl;

namespace ReportingService.Services
{
    public interface IReportingService
    {
        Task<List<User>> GetUserSummaryAsync();
        Task<List<Product>> GetTopProductsAsync();
        Task<List<Order>> GetOrdersAsync(DateTime? startDate, DateTime? endDate);
        Task<FileResultModel> ExportOrdersAsCsvAsync(DateTime? startDate, DateTime? endDate);
    }
}
