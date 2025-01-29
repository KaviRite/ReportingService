using Microsoft.EntityFrameworkCore;
using ReportingService.Data;
using ReportingService.Services;
using NLog;
using NLog.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Globalization;

var nLogger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();

try
{
    nLogger.Debug("Initializing Application..");
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddDbContext<ReportingDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Register ReportingService and IReportingService
    builder.Services.AddScoped<IReportingService, ReportingServiceImpl>();

    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Reporting Service API",
            Version = "v1",
            Description = "API Documentation for Reporting Service"
        });

        // Add JWT Authentication to Swagger
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter 'Bearer' [space] and then your valid JWT token."
        });

        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });
    });

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
            };

            // Adding event handlers for debugging and customization
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Console.WriteLine($"Token validated successfully for user: {context.Principal.Identity.Name}");
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    Console.WriteLine("Authorization challenge occurred.");
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("JwtPolicy", policy =>
        {
            policy.RequireAuthenticatedUser();
        });
    });

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // This will help prevent circular reference issues when serializing objects
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        });

    // Enable static file serving (HTML, CSS, JS files in wwwroot)
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Serve static files (like HTML, CSS, and JS)
    app.UseStaticFiles(); // Serves files from wwwroot folder

    // API to fetch the summary of users.
    app.MapGet("/api/users-summary", async (IReportingService reportingService) =>
    {
        try
        {
            var users = await reportingService.GetUserSummaryAsync();
            nLogger.Trace("Called API to fetch user summary.");
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
    app.MapGet("/api/top-products", async (IReportingService reportingService) =>
    {
        try
        {
            var products = await reportingService.GetTopProductsAsync();
            nLogger.Trace("Called API to fetch top products.");
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
    app.MapGet("/api/export-csv", async (IReportingService reportingService, string? startDate, string? endDate) =>
    {
        try
        {
            // Parse the dates from query parameters
            DateTime? start = string.IsNullOrEmpty(startDate) ? null : DateTime.Parse(startDate);
            DateTime? end = string.IsNullOrEmpty(endDate) ? null : DateTime.Parse(endDate);

            var fileResult = await reportingService.ExportOrdersAsCsvAsync(start, end);
            nLogger.Trace("Called API to export report.");
            return Results.File(fileResult.Content, fileResult.ContentType, fileResult.FileName);
        }
        catch (Exception ex)
        {
            nLogger.Error(ex, "Error exporting csv report");
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
}
catch (Exception e)
{
    nLogger.Fatal(e, "Application Startup Failed");
    throw;
}
finally
{
    LogManager.Shutdown();
}
