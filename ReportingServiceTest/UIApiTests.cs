using Microsoft.AspNetCore.Mvc;
using Moq;
using ReportingService.Controllers;
using ReportingService.Models;
using ReportingService.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static ReportingService.Services.ReportingServiceImpl;

public class UIApiTests
{
    private readonly Mock<IReportingService> _mockReportingService;

    public UIApiTests()
    {
        _mockReportingService = new Mock<IReportingService>();
    }

    [Fact]
    public async Task GetUserSummary_ReturnsOkResult_WhenDataExists()
    {
        // Arrange: Create mock data for user summary
        var users = new List<User>
        {
            new User { UserId = 1, UserName = "John Doe", Contact = 7894561238, Gender="Male", Email="john@abc.com", DOB= new DateTime(1999, 01, 01), Address = new Address { ShippingAddress = "123 Main St", BillingAddress = "123 Main St", City = "New York", State = "Washington" } }
        };
        _mockReportingService.Setup(service => service.GetUserSummaryAsync()).ReturnsAsync(users);

        // Arrange: Create controller instance
        var controller = new ReportingController(_mockReportingService.Object);

        // Act: Call the GetUserSummary method
        var result = await controller.GetUserSummary();

        // Assert: Verify that the returned result is OkObjectResult
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUsers = Assert.IsAssignableFrom<List<User>>(okResult.Value);
        Assert.Equal(1, returnedUsers.Count);
        Assert.Equal("John Doe", returnedUsers[0].UserName);
    }

    [Fact]
    public async Task GetTopProducts_ReturnsOkResult_WhenDataExists()
    {
        // Arrange: Mock data for top products
        var products = new List<Product>
        {
            new Product { ProductId = 1, Description = "Product A", Price = 100, InStock = 10, OrdersReceived = 10 },
            new Product { ProductId = 2, Description = "Product B", Price = 200, InStock = 15, OrdersReceived = 20 }
        };
        _mockReportingService.Setup(service => service.GetTopProductsAsync()).ReturnsAsync(products);

        // Arrange: Create controller instance
        var controller = new ReportingController(_mockReportingService.Object);

        // Act: Call the GetTopProducts method
        var result = await controller.GetTopProducts();

        // Assert: Verify the response is OkObjectResult
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedProducts = Assert.IsAssignableFrom<List<Product>>(okResult.Value);
        Assert.Equal(2, returnedProducts.Count);
        Assert.Equal("Product A", returnedProducts[0].Description); // Sorted by OrdersReceived
    }

    [Fact]
    public async Task ExportCsvReport_ReturnsFileResult_WhenValidDatesProvided()
    {
        // Arrange: Create mock data for CSV export
        var fileResultModel = new FileResultModel
        {
            Content = System.Text.Encoding.UTF8.GetBytes("OrderId,UserId,UserName,ProductId,ProductDescription,Price,QuantityOrdered,PurchaseDate,Region\n1,1,John Doe,1,Product A,100,2,2025-01-01,New York"),
            ContentType = "text/csv",
            FileName = "Order_Report_2025-01-01.csv"
        };
        _mockReportingService.Setup(service => service.ExportOrdersAsCsvAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                             .ReturnsAsync(fileResultModel);

        // Arrange: Create controller instance
        var controller = new ReportingController(_mockReportingService.Object);

        // Act: Call the ExportReportAsCsv method
        var result = await controller.ExportReportAsCsv(null, null);

        // Assert: Verify the response is FileContentResult
        var fileResultReturned = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResultReturned.ContentType);
        var csvString = System.Text.Encoding.UTF8.GetString(fileResultReturned.FileContents);
        Assert.Contains("OrderId,UserId,UserName,ProductId,ProductDescription,Price,QuantityOrdered,PurchaseDate,Region", csvString);
    }

    [Fact]
    public async Task ExportCsvReport_StartDateGreaterThanEndDate_ReturnsBadRequest()
    {
        // Arrange: Invalid date range
        var startDate = new DateTime(2024, 12, 31);
        var endDate = new DateTime(2024, 1, 1);

        // Arrange: Create controller instance
        var controller = new ReportingController(_mockReportingService.Object);

        // Act: Call the ExportReportAsCsv method with invalid dates
        var result = await controller.ExportReportAsCsv(startDate, endDate);

        // Assert: Verify the result is BadRequest
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
        Assert.Contains("Start date cannot be greater than end date", badRequestResult.Value.ToString());
    }
}
