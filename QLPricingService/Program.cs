using Microsoft.EntityFrameworkCore;
using QLPricingService.Data;
using QLPricingService.Features.CalculatePrice;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// --- Service Registration ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "QL Pricing API",
        Description = "API for calculating service prices for customers."
    });

    // Look for XML documentation file
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation(); // Automatically registers validators & enables MVC integration
builder.Services.AddValidatorsFromAssemblyContaining<CalculatePriceQueryValidator>(); // Scan assembly for validators

// Add DbContext
builder.Services.AddDbContext<PricingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Feature Handlers
builder.Services.AddScoped<QLPricingService.Features.CalculatePrice.Handler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "QL Pricing API v1");
    });
}

app.UseHttpsRedirection();

// Define Pricing Endpoint
app.MapGet("/pricing", async (
    [FromQuery] int customerId, 
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    QLPricingService.Features.CalculatePrice.Handler handler,
    CancellationToken cancellationToken)
    =>
{
    var query = new CalculatePriceQuery(customerId, startDate, endDate);
    
    var (response, errorMessage, statusCode) = await handler.HandleAsync(query, cancellationToken);

    return statusCode switch
    {
        HttpStatusCode.OK => Results.Ok(response),
        HttpStatusCode.NotFound => Results.NotFound(new { Message = errorMessage }),
        HttpStatusCode.BadRequest => Results.BadRequest(new { Message = errorMessage }),
        HttpStatusCode.InternalServerError => Results.Problem(detail: errorMessage, statusCode: (int)statusCode),
        _ => Results.Problem(detail: "An unexpected error occurred.", statusCode: 500)
    };
})
.WithName("CalculatePrice")
.WithSummary("Calculate Customer Price")
.WithDescription("Calculates the total price for a customer over a specified period, considering services used, discounts, and free days.")
.Produces<CalculatePriceResponse>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

// Define Pricing Endpoint By Name
app.MapGet("/pricing/by-name", async (
    [FromQuery] string customerName,
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    QLPricingService.Features.CalculatePrice.Handler handler, // Reuse existing handler
    PricingDbContext dbContext, // Inject DbContext for name lookup
    CancellationToken cancellationToken)
    =>
{
    if (string.IsNullOrWhiteSpace(customerName))
    {
        return Results.BadRequest(new { Message = "Customer name cannot be empty." });
    }

    // Find customer by name
    var customer = await dbContext.Customers
                                  .AsNoTracking() // Read-only query
                                  .FirstOrDefaultAsync(c => c.Name == customerName, cancellationToken);

    if (customer == null)
    {
        return Results.NotFound(new { Message = $"Customer with name '{customerName}' not found." });
    }

    // Use the found customer ID to call the existing handler
    var query = new CalculatePriceQuery(customer.Id, startDate, endDate);
    var (response, errorMessage, statusCode) = await handler.HandleAsync(query, cancellationToken);

    // Map the result just like the other endpoint
    return statusCode switch
    {
        HttpStatusCode.OK => Results.Ok(response),
        HttpStatusCode.NotFound => Results.NotFound(new { Message = errorMessage }), // Should not happen if ID was just found, but handle defensively
        HttpStatusCode.BadRequest => Results.BadRequest(new { Message = errorMessage }),
        HttpStatusCode.InternalServerError => Results.Problem(detail: errorMessage, statusCode: (int)statusCode),
        _ => Results.Problem(detail: "An unexpected error occurred.", statusCode: 500)
    };
})
.WithName("CalculatePriceByName")
.WithSummary("Calculate Customer Price By Name")
.WithDescription("Calculates the total price for a customer identified by name over a specified period.")
.Produces<CalculatePriceResponse>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.Run();
