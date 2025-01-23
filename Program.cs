using Microsoft.EntityFrameworkCore;
using ReportingService.Data;

using NLog;
using NLog.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Globalization;

var nLogger = NLog.LogManager.GetCurrentClassLogger();

try
{
    nLogger.Debug("Initializing Application..");
    var builder = WebApplication.CreateBuilder(args);
   

    // Add services to the container.
    builder.Services.AddDbContext<ReportingDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    //JWT Authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
    builder.Services.AddControllers();

    // Enable static file serving (HTML, CSS, JS files in wwwroot)
    var app = builder.Build();

    // Serve static files (like HTML, CSS, and JS)
    app.UseStaticFiles(); // Serves files from wwwroot folder

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    //app.UseAuthentication();
    //app.UseAuthorization();

    // API to fetch the summary of users.
    app.MapGet("/api/users-summary", async (ReportingDbContext context) =>
    {
        try
        {
            var users = await context.Users
            .Include(u => u.Address)
            .ToListAsync();
            return Results.Ok(users);
        }
        catch (Exception ex)
        {
            nLogger.Error(ex, "Error fetching user Summary");
            return Results.Problem("An error occurred while fetching user summary.");
        }
    })
    .WithName("GetUsersSummary");

    // API to fetch the list of top products by the number of orders received.
    app.MapGet("/api/top-products", async (ReportingDbContext context) =>
    {
        try
        {
            var products = await context.Products
                .OrderByDescending(p => p.OrdersReceived)
                .ToListAsync();
            return Results.Ok(products);
        }
        catch (Exception ex)
        {
            nLogger.Error(ex, "Error fetching top products");
            return Results.Problem("An error occurred while fetching top products.");
        }
    })
    .WithName("GetTopProducts");

    // API to export the orders report as a CSV file.
    app.MapGet("/api/export-csv", async (ReportingDbContext context, string startDate, string endDate) =>
    {
        try
        {
            // Parse the dates from query parameters
            DateTime? start = string.IsNullOrEmpty(startDate) ? null : DateTime.Parse(startDate);
            DateTime? end = string.IsNullOrEmpty(endDate) ? null : DateTime.Parse(endDate);

            var ordersQuery = context.Orders
                .Include(o => o.User)
                .Include(o => o.User.Address)
                .Include(o => o.Product)
                .AsQueryable();

            if (start.HasValue)
                ordersQuery = ordersQuery.Where(o => o.PurchaseDate >= start.Value);

            if (end.HasValue)
                ordersQuery = ordersQuery.Where(o => o.PurchaseDate <= end.Value);

            var orders = await ordersQuery.ToListAsync();

            var csvBuilder = new System.Text.StringBuilder();
            csvBuilder.AppendLine("OrderId,UserId,UserName,ProductId,ProductDescription,ProductPrice,ProductQuantity,PurchaseDate,Region");

            foreach (var order in orders)
            {
                csvBuilder.AppendLine($"{order.OrderId},{order.User.UserId},{order.User.UserName},{order.Product.ProductId},{order.Product.Description},{order.Product.Price},{order.QtyOrdered},{order.PurchaseDate:yyyy-MM-dd},{order.User.Address.City}");
            }

            var csvBytes = System.Text.Encoding.UTF8.GetBytes(csvBuilder.ToString());
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"log_{timestamp}.csv";
            return Results.File(csvBytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            NLog.LogManager.GetCurrentClassLogger().Error(ex, "Error exporting csv report");
            return Results.Problem("An Error occurred while exporting csv report");
        }
    })
    .WithName("ExportCsvReport");


    // Serve index.html as the default page when accessing the root URL
    app.MapFallbackToFile("index.html"); // Serves index.html in wwwroot as fallback

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
} catch (Exception e)
{
    nLogger.Fatal(e,"Application Startup Failed" );
    throw;
}
finally
{
    LogManager.Shutdown();
}
