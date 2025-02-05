using Microsoft.AspNetCore.Mvc;
using Moq;
using ReportingService.Controllers;  // Adjust to your namespace
using ReportingService.Models;       // Adjust to your Models namespace
using ReportingService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static ReportingService.Services.ReportingServiceImpl;

public class ReportingControllerTests
{
    private readonly ReportingController _controller;
    private readonly Mock<IReportingService> _mockReportingService;

    public ReportingControllerTests()
    {
        // Arrange: Mock IReportingService
        _mockReportingService = new Mock<IReportingService>();
        _controller = new ReportingController(_mockReportingService.Object);
    }

    [Fact]
    public async Task GetUserSummaryReturnsOkResultWhenDataExists()
    {
        // Arrange: Mock GetUserSummaryAsync method
        var users = new List<User>
        {
            new User { UserId = 1, UserName = "John Doe", Contact = 7894561238, Gender="Male", Email="john@abc.com", DOB= new DateTime(1999, 01, 01), Address = new Address { ShippingAddress = "123 Main St", BillingAddress = "123 Main St", City = "New York", State = "Washington" } },
        };
        _mockReportingService.Setup(service => service.GetUserSummaryAsync()).ReturnsAsync(users);

        // Act: Call the GetUserSummary method
        var result = await _controller.GetUserSummary();

        // Assert: Check that we got an OkResult
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Assert: Ensure the result is of type List<User>
        var returnedUsers = Assert.IsAssignableFrom<List<User>>(okResult.Value);

        // Assert: Verify the list contains 1 user
        Assert.Equal(1, returnedUsers.Count);

        // Assert: Ensure user properties are correct
        var user1 = returnedUsers[0];
        Assert.Equal("John Doe", user1.UserName);
        Assert.Equal("123 Main St", user1.Address.ShippingAddress);
    }

    [Fact]
    public async Task GetUserSummaryReturnsInternalServerErrorWhenExceptionOccurs()
    {
        // Arrange: Mock GetUserSummaryAsync method to throw an exception
        _mockReportingService.Setup(service => service.GetUserSummaryAsync()).ThrowsAsync(new Exception("Test exception"));

        // Act: Call the GetUserSummary method
        var result = await _controller.GetUserSummary();

        // Assert: Check that we got an InternalServerError result
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Contains("An error occurred while fetching user summary", statusCodeResult.Value.ToString());
    }


    [Fact]
    public async Task GetTopProductsReturnsOkResultWhenDataExists()
    {
        // Arrange: Mock GetTopProductsAsync method
        var products = new List<Product>
        {
            new Product { ProductId = 1, Description = "Product A", Price = 100, InStock = 10, OrdersReceived = 10 },
            new Product { ProductId = 2, Description = "Product B", Price = 200, InStock = 15, OrdersReceived = 20 },
        };
        _mockReportingService.Setup(service => service.GetTopProductsAsync()).ReturnsAsync(products);

        // Act: Call the GetTopProducts method
        var result = await _controller.GetTopProducts();

        // Assert: Check that we got an OkResult
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Assert: Ensure the result is of type List<Product>
        var returnedProducts = Assert.IsAssignableFrom<List<Product>>(okResult.Value);

        // Assert: Verify the list is sorted by OrdersReceived
        Assert.Equal(2, returnedProducts.Count);
        Assert.Equal("Product A", returnedProducts[0].Description); // Highest orders first
        Assert.Equal("Product B", returnedProducts[1].Description);
    }

    [Fact]
    public async Task GetTopProductsReturnsInternalServerErrorWhenExceptionOccurs()
    {
        // Arrange: Mock GetTopProductsAsync method to throw an exception
        _mockReportingService.Setup(service => service.GetTopProductsAsync()).ThrowsAsync(new Exception("Test exception"));

        // Act: Call the GetTopProducts method
        var result = await _controller.GetTopProducts();

        // Assert: Check that we got an InternalServerError result
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Contains("An error occurred while fetching top products", statusCodeResult.Value.ToString());
    }


    [Fact]
    public async Task ExportReportAsCsvReturnsFileWithCorrectContent()
    {
        // Arrange: Create the expected result (FileResultModel)
        var csvContent = "OrderId,UserId,UserName,ProductId,ProductDescription,Price,QuantityOrdered,PurchaseDate,Region\n" +
                         "1,1,John Doe,1,Product A,100,2,2025-01-01,New York";

        var fileResultModel = new FileResultModel
        {
            Content = Encoding.UTF8.GetBytes(csvContent),
            ContentType = "text/csv",
            FileName = $"Order_Report_2025-01-01.csv"
        };

        // Mock ExportOrdersAsCsvAsync method
        _mockReportingService.Setup(service => service.ExportOrdersAsCsvAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                             .ReturnsAsync(fileResultModel);

        // Act: Call the ExportReportAsCsv method
        var result = await _controller.ExportReportAsCsv(null, null);

        // Assert: Check that we got a FileContentResult
        var fileResultReturned = Assert.IsType<FileContentResult>(result);

        // Assert: Ensure the file has correct MIME type and content
        Assert.Equal("text/csv", fileResultReturned.ContentType);
        var csvString = Encoding.UTF8.GetString(fileResultReturned.FileContents);
        Assert.Contains("OrderId,UserId,UserName,ProductId,ProductDescription,Price,QuantityOrdered,PurchaseDate,Region", csvString);
        Assert.Contains("1,1,John Doe,1,Product A,100,2,2025-01-01,New York", csvString);
    }

    [Fact]
    public async Task ExportReportAsCsv_StartDateGreaterThanEndDate_ReturnsBadRequest()
    {
        // Arrange
        DateTime startDate = new DateTime(2024, 12, 31);
        DateTime endDate = new DateTime(2024, 1, 1);

        // Act
        var result = await _controller.ExportReportAsCsv(startDate, endDate);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
        Assert.Contains("Start date cannot be greater than end date", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task ExportReportAsCsvWithNullDatesReturnsFileResult()
    {
        // Arrange: Create the expected result (FileResultModel)
        var csvContent = "OrderId,UserId,UserName,ProductId,ProductDescription,Price,QuantityOrdered,PurchaseDate,Region\n" +
                         "1,1,John Doe,1,Product A,100,2,2025-01-01,New York";

        var fileResultModel = new FileResultModel
        {
            Content = Encoding.UTF8.GetBytes(csvContent),
            ContentType = "text/csv",
            FileName = $"Order_Report_2025-01-01.csv"
        };

        // Mock ExportOrdersAsCsvAsync method
        _mockReportingService.Setup(service => service.ExportOrdersAsCsvAsync(null, null))
                             .ReturnsAsync(fileResultModel);

        // Act: Call the ExportReportAsCsv method with null dates
        var result = await _controller.ExportReportAsCsv(null, null);

        // Assert: Check that we got a FileContentResult
        var fileResultReturned = Assert.IsType<FileContentResult>(result);

        // Assert: Ensure the file has correct MIME type and content
        Assert.Equal("text/csv", fileResultReturned.ContentType);
        var csvString = Encoding.UTF8.GetString(fileResultReturned.FileContents);
        Assert.Contains("OrderId,UserId,UserName,ProductId,ProductDescription,Price,QuantityOrdered,PurchaseDate,Region", csvString);
        Assert.Contains("1,1,John Doe,1,Product A,100,2,2025-01-01,New York", csvString);
    }

    [Fact]
    public async Task ExportReportAsCsvWithoutDateParamsReturnsFileResult()
    {
        // Arrange: Create the expected result (FileResultModel)
        var csvContent = "OrderId,UserId,UserName,ProductId,ProductDescription,Price,QuantityOrdered,PurchaseDate,Region\n" +
                         "1,1,John Doe,1,Product A,100,2,2025-01-01,New York";

        var fileResultModel = new FileResultModel
        {
            Content = Encoding.UTF8.GetBytes(csvContent),
            ContentType = "text/csv",
            FileName = $"Order_Report_2025-01-01.csv"
        };

        // Mock ExportOrdersAsCsvAsync method without date parameters
        _mockReportingService.Setup(service => service.ExportOrdersAsCsvAsync(null, null))
                             .ReturnsAsync(fileResultModel);

        // Act: Call the ExportReportAsCsv method without date parameters
        var result = await _controller.ExportReportAsCsv(null, null);

        // Assert: Check that we got a FileContentResult
        var fileResultReturned = Assert.IsType<FileContentResult>(result);

        // Assert: Ensure the file has correct MIME type and content
        Assert.Equal("text/csv", fileResultReturned.ContentType);
        var csvString = Encoding.UTF8.GetString(fileResultReturned.FileContents);
        Assert.Contains("OrderId,UserId,UserName,ProductId,ProductDescription,Price,QuantityOrdered,PurchaseDate,Region", csvString);
        Assert.Contains("1,1,John Doe,1,Product A,100,2,2025-01-01,New York", csvString);
    }

    [Fact]
    public async Task ExportReportAsCsvWithInvalidDateFormatReturnsBadRequest()
    {
        // Arrange: Set invalid date format for startDate and endDate
        string invalidStartDate = "invalid-date";
        string invalidEndDate = "invalid-date";

        // Act: Call the ExportReportAsCsv method with invalid date format
        var result = await _controller.ExportReportAsCsv(
            DateTime.TryParse(invalidStartDate, out var startDate) ? startDate : (DateTime?)null,
            DateTime.TryParse(invalidEndDate, out var endDate) ? endDate : (DateTime?)null
        );

        // Assert: Check that we got a BadRequest result
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Contains("An error occurred while exporting CSV report", statusCodeResult.Value.ToString());
    }


}
