using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using QLPricingService.Data;
using QLPricingService.Domain;
using QLPricingService.Features.CalculatePrice;
using System.Net;

namespace QLPricingService.Tests.Features.CalculatePrice;

// Implement IDisposable to clean up the in-memory database after each test
public class HandlerTests : IDisposable
{
    private readonly PricingDbContext _dbContext;
    private readonly Mock<ILogger<Handler>> _mockLogger;
    private readonly Handler _handler;

    public HandlerTests()
    {
        // Use InMemory database provider
        var options = new DbContextOptionsBuilder<PricingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique name per test run
            .Options;
        _dbContext = new PricingDbContext(options);

        _mockLogger = new Mock<ILogger<Handler>>();
        _handler = new Handler(_dbContext, _mockLogger.Object);

        // Seed initial Service data needed for most tests
        SeedServices();
    }

    private void SeedServices()
    {
        _dbContext.Services.AddRange(
            new Service { Id = 1, Name = "Service A", BasePricePerDay = 0.2m, ChargesOnWeekends = false },
            new Service { Id = 2, Name = "Service B", BasePricePerDay = 0.24m, ChargesOnWeekends = false },
            new Service { Id = 3, Name = "Service C", BasePricePerDay = 0.4m, ChargesOnWeekends = true }
        );
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    // --- Test Methods --- 

    [Fact]
    public async Task HandleAsync_InvalidDateRange_ReturnsBadRequest()
    {
        var query = new CalculatePriceQuery(1, new DateTime(2023, 1, 10), new DateTime(2023, 1, 1));

        var result = await _handler.HandleAsync(query);

        Assert.Null(result.Response);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("EndDate must be on or after StartDate.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_CustomerNotFound_ReturnsNotFound()
    {
        var customerId = 999;
        var query = new CalculatePriceQuery(customerId, new DateTime(2023, 1, 1), new DateTime(2023, 1, 10));
        // Note: Database is empty except for Services, so customer 999 won't be found

        var (_, errorMessage, statusCode) = await _handler.HandleAsync(query);

        Assert.Equal(HttpStatusCode.NotFound, statusCode);
        Assert.Contains("not found", errorMessage);
        Assert.Contains(customerId.ToString(), errorMessage);
    }

    [Fact]
    public async Task HandleAsync_TestCase1Logic_ReturnsCorrectPrice()
    {
        int customerId = 1;
        var startDate = new DateTime(2019, 9, 20);
        var endDate = new DateTime(2019, 9, 30);
        var expectedTotalPrice = 5.56m; // Based on previous correct calculation

        _dbContext.Customers.Add(new Customer { Id = customerId, GlobalFreeDays = 0 });
        _dbContext.CustomerServiceUsages.AddRange(
            new CustomerServiceUsage { Id = 1, CustomerId = customerId, ServiceId = 1, StartDate = new DateTime(2019, 9, 20) },
            new CustomerServiceUsage { Id = 2, CustomerId = customerId, ServiceId = 3, StartDate = new DateTime(2019, 9, 20) }
        );
        _dbContext.Discounts.Add(new Discount
        {
            Id = 1,
            CustomerId = customerId,
            ServiceId = 3, // Discount for Service C
            Percentage = 0.20m,
            StartDate = new DateTime(2019, 9, 22),
            EndDate = new DateTime(2019, 9, 24)
        });
        await _dbContext.SaveChangesAsync();

        var query = new CalculatePriceQuery(customerId, startDate, endDate);

        var (response, errorMessage, statusCode) = await _handler.HandleAsync(query);

        Assert.Equal(HttpStatusCode.OK, statusCode);
        Assert.Null(errorMessage);
        Assert.NotNull(response);
        Assert.Equal(expectedTotalPrice, response.TotalPrice, precision: 2);
    }

    [Fact]
    public async Task HandleAsync_TestCase2Logic_ReturnsCorrectPrice()
    {
        int customerId = 2;
        var startDate = new DateTime(2018, 1, 1);
        var endDate = new DateTime(2019, 9, 30);
        var expectedTotalPrice = 196.224m; // Based on previous correct calculation

        _dbContext.Customers.Add(new Customer { Id = customerId, GlobalFreeDays = 200 });
        _dbContext.CustomerServiceUsages.AddRange(
            new CustomerServiceUsage { Id = 3, CustomerId = customerId, ServiceId = 2, StartDate = new DateTime(2018, 1, 1) },
            new CustomerServiceUsage { Id = 4, CustomerId = customerId, ServiceId = 3, StartDate = new DateTime(2018, 1, 1) }
        );
        _dbContext.Discounts.AddRange(
            new Discount // 30% discount for Service B
            {
                Id = 2,
                CustomerId = customerId,
                ServiceId = 2,
                Percentage = 0.30m,
                StartDate = new DateTime(2018, 1, 1),
                EndDate = new DateTime(2099, 12, 31)
            },
            new Discount // 30% discount for Service C
            {
                Id = 3,
                CustomerId = customerId,
                ServiceId = 3,
                Percentage = 0.30m,
                StartDate = new DateTime(2018, 1, 1),
                EndDate = new DateTime(2099, 12, 31)
            }
        );
        await _dbContext.SaveChangesAsync();

        var query = new CalculatePriceQuery(customerId, startDate, endDate);

        var (response, errorMessage, statusCode) = await _handler.HandleAsync(query);

        Assert.Equal(HttpStatusCode.OK, statusCode);
        Assert.Null(errorMessage);
        Assert.NotNull(response);
        Assert.Equal(expectedTotalPrice, response.TotalPrice, precision: 3);
    }

    // TODO: Add more granular tests for specific discount logic, free days, working days etc.

    [Fact]
    public async Task HandleAsync_ServiceWithoutWeekendCharging_SkipsWeekends()
    {
        int customerId = 3;
        var startDate = new DateTime(2023, 10, 23); // Monday
        var endDate = new DateTime(2023, 10, 29);   // Sunday
        // Period: Mon, Tue, Wed, Thu, Fri, Sat, Sun (7 days total)
        // Service A (0.2/day, no weekend charge) should charge for 5 days.
        var expectedTotalPrice = 5 * 0.2m;

        _dbContext.Customers.Add(new Customer { Id = customerId, GlobalFreeDays = 0 });
        _dbContext.CustomerServiceUsages.Add(
            new CustomerServiceUsage { CustomerId = customerId, ServiceId = 1, StartDate = startDate } // Service A
        );
        await _dbContext.SaveChangesAsync();

        var query = new CalculatePriceQuery(customerId, startDate, endDate);

        var (response, _, statusCode) = await _handler.HandleAsync(query);

        Assert.Equal(HttpStatusCode.OK, statusCode);
        Assert.NotNull(response);
        Assert.Equal(expectedTotalPrice, response.TotalPrice, precision: 2);
    }

    [Fact]
    public async Task HandleAsync_ServiceWithWeekendCharging_IncludesWeekends()
    {
        int customerId = 4;
        var startDate = new DateTime(2023, 10, 23); // Monday
        var endDate = new DateTime(2023, 10, 29);   // Sunday
        // Period: Mon, Tue, Wed, Thu, Fri, Sat, Sun (7 days total)
        // Service C (0.4/day, charges weekends) should charge for 7 days.
        var expectedTotalPrice = 7 * 0.4m;

        _dbContext.Customers.Add(new Customer { Id = customerId, GlobalFreeDays = 0 });
        _dbContext.CustomerServiceUsages.Add(
            new CustomerServiceUsage { CustomerId = customerId, ServiceId = 3, StartDate = startDate } // Service C
        );
        await _dbContext.SaveChangesAsync();

        var query = new CalculatePriceQuery(customerId, startDate, endDate);

        var (response, _, statusCode) = await _handler.HandleAsync(query);

        Assert.Equal(HttpStatusCode.OK, statusCode);
        Assert.NotNull(response);
        Assert.Equal(expectedTotalPrice, response.TotalPrice, precision: 2);
    }

    // --- Discount Tests ---

    [Fact]
    public async Task HandleAsync_DiscountStartsMidPeriod_AppliesCorrectly()
    {
        int customerId = 5;
        var queryStartDate = new DateTime(2023, 11, 1); // Wed
        var queryEndDate = new DateTime(2023, 11, 10);  // Fri (next week)
        // Total 10 days. Service C (0.4/day, charges weekends)
        var discountStartDate = new DateTime(2023, 11, 6); // Monday
        var discountEndDate = new DateTime(2023, 11, 10); // Fri
        var discountPercentage = 0.50m; // 50% discount

        // Days 1-5 (Nov 1-5): Full price (0.4 * 5 days = 2.0)
        // Days 6-10 (Nov 6-10): Discounted price (0.4 * 0.5 * 5 days = 1.0)
        var expectedTotalPrice = (5 * 0.4m) + (5 * 0.4m * (1 - discountPercentage));

        _dbContext.Customers.Add(new Customer { Id = customerId, GlobalFreeDays = 0 });
        _dbContext.CustomerServiceUsages.Add(new CustomerServiceUsage { CustomerId = customerId, ServiceId = 3, StartDate = queryStartDate }); // Service C
        _dbContext.Discounts.Add(new Discount
        {
            CustomerId = customerId,
            ServiceId = 3,
            Percentage = discountPercentage,
            StartDate = discountStartDate,
            EndDate = discountEndDate
        });
        await _dbContext.SaveChangesAsync();

        var query = new CalculatePriceQuery(customerId, queryStartDate, queryEndDate);

        var (response, _, statusCode) = await _handler.HandleAsync(query);

        Assert.Equal(HttpStatusCode.OK, statusCode);
        Assert.NotNull(response);
        Assert.Equal(expectedTotalPrice, response.TotalPrice, precision: 3);
    }

    [Fact]
    public async Task HandleAsync_DiscountEndsMidPeriod_AppliesCorrectly()
    {
        int customerId = 6;
        var queryStartDate = new DateTime(2023, 11, 1); // Wed
        var queryEndDate = new DateTime(2023, 11, 10);  // Fri (next week)
        // Total 10 days. Service C (0.4/day, charges weekends)
        var discountStartDate = new DateTime(2023, 11, 1); // Wed
        var discountEndDate = new DateTime(2023, 11, 5); // Sun
        var discountPercentage = 0.25m; // 25% discount

        // Days 1-5 (Nov 1-5): Discounted price (0.4 * 0.75 * 5 days = 1.5)
        // Days 6-10 (Nov 6-10): Full price (0.4 * 5 days = 2.0)
        var expectedTotalPrice = (5 * 0.4m * (1 - discountPercentage)) + (5 * 0.4m);

        _dbContext.Customers.Add(new Customer { Id = customerId, GlobalFreeDays = 0 });
        _dbContext.CustomerServiceUsages.Add(new CustomerServiceUsage { CustomerId = customerId, ServiceId = 3, StartDate = queryStartDate }); // Service C
        _dbContext.Discounts.Add(new Discount
        {
            CustomerId = customerId,
            ServiceId = 3,
            Percentage = discountPercentage,
            StartDate = discountStartDate,
            EndDate = discountEndDate
        });
        await _dbContext.SaveChangesAsync();

        var query = new CalculatePriceQuery(customerId, queryStartDate, queryEndDate);

        var (response, _, statusCode) = await _handler.HandleAsync(query);

        Assert.Equal(HttpStatusCode.OK, statusCode);
        Assert.NotNull(response);
        Assert.Equal(expectedTotalPrice, response.TotalPrice, precision: 3);
    }

    [Fact]
    public async Task HandleAsync_DiscountCoversFullPeriod_AppliesCorrectly()
    {
        int customerId = 7;
        var queryStartDate = new DateTime(2023, 11, 1); // Wed
        var queryEndDate = new DateTime(2023, 11, 10);  // Fri (next week)
        // Total 10 days. Service C (0.4/day, charges weekends)
        var discountStartDate = new DateTime(2023, 11, 1);
        var discountEndDate = new DateTime(2023, 11, 10);
        var discountPercentage = 0.10m; // 10% discount

        // All 10 days discounted: (0.4 * 0.9 * 10 days = 3.6)
        var expectedTotalPrice = 10 * 0.4m * (1 - discountPercentage);

        _dbContext.Customers.Add(new Customer { Id = customerId, GlobalFreeDays = 0 });
        _dbContext.CustomerServiceUsages.Add(new CustomerServiceUsage { CustomerId = customerId, ServiceId = 3, StartDate = queryStartDate }); // Service C
        _dbContext.Discounts.Add(new Discount
        {
            CustomerId = customerId,
            ServiceId = 3,
            Percentage = discountPercentage,
            StartDate = discountStartDate,
            EndDate = discountEndDate
        });
        await _dbContext.SaveChangesAsync();

        var query = new CalculatePriceQuery(customerId, queryStartDate, queryEndDate);

        var (response, _, statusCode) = await _handler.HandleAsync(query);

        Assert.Equal(HttpStatusCode.OK, statusCode);
        Assert.NotNull(response);
        Assert.Equal(expectedTotalPrice, response.TotalPrice, precision: 3);
    }

    // --- Global Free Days Tests ---

    [Fact]
    public async Task HandleAsync_FreeDaysLessThanChargeable_ReducesPrice()
    {
        int customerId = 8;
        var queryStartDate = new DateTime(2023, 12, 1); // Fri
        var queryEndDate = new DateTime(2023, 12, 10);  // Sun
        // Total 10 days. Service C (0.4/day, charges weekends)
        int freeDays = 3;
        // Expected price = (10 chargeable days - 3 free days) * 0.4 = 7 * 0.4 = 2.8
        var expectedTotalPrice = (10 - freeDays) * 0.4m;

        _dbContext.Customers.Add(new Customer { Id = customerId, GlobalFreeDays = freeDays });
        _dbContext.CustomerServiceUsages.Add(new CustomerServiceUsage { CustomerId = customerId, ServiceId = 3, StartDate = queryStartDate }); // Service C
        await _dbContext.SaveChangesAsync();

        var query = new CalculatePriceQuery(customerId, queryStartDate, queryEndDate);

        var (response, _, statusCode) = await _handler.HandleAsync(query);

        Assert.Equal(HttpStatusCode.OK, statusCode);
        Assert.NotNull(response);
        Assert.Equal(expectedTotalPrice, response.TotalPrice, precision: 3);
    }

    [Fact]
    public async Task HandleAsync_FreeDaysEqualToChargeable_ReturnsZeroPrice()
    {
        int customerId = 9;
        var queryStartDate = new DateTime(2023, 12, 1); // Fri
        var queryEndDate = new DateTime(2023, 12, 10);  // Sun
        // Total 10 days. Service C (0.4/day, charges weekends)
        int freeDays = 10;
        // Expected price = (10 chargeable days - 10 free days) * 0.4 = 0
        var expectedTotalPrice = 0m;

        _dbContext.Customers.Add(new Customer { Id = customerId, GlobalFreeDays = freeDays });
        _dbContext.CustomerServiceUsages.Add(new CustomerServiceUsage { CustomerId = customerId, ServiceId = 3, StartDate = queryStartDate }); // Service C
        await _dbContext.SaveChangesAsync();

        var query = new CalculatePriceQuery(customerId, queryStartDate, queryEndDate);

        var (response, _, statusCode) = await _handler.HandleAsync(query);

        Assert.Equal(HttpStatusCode.OK, statusCode);
        Assert.NotNull(response);
        Assert.Equal(expectedTotalPrice, response.TotalPrice, precision: 3);
    }

    [Fact]
    public async Task HandleAsync_FreeDaysMoreThanChargeable_ReturnsZeroPrice()
    {
        int customerId = 10;
        var queryStartDate = new DateTime(2023, 12, 1); // Fri
        var queryEndDate = new DateTime(2023, 12, 10);  // Sun
        // Total 10 days. Service C (0.4/day, charges weekends)
        int freeDays = 15;
        // Expected price = Max(0, 10 chargeable days - 15 free days) * 0.4 = 0
        var expectedTotalPrice = 0m;

        _dbContext.Customers.Add(new Customer { Id = customerId, GlobalFreeDays = freeDays });
        _dbContext.CustomerServiceUsages.Add(new CustomerServiceUsage { CustomerId = customerId, ServiceId = 3, StartDate = queryStartDate }); // Service C
        await _dbContext.SaveChangesAsync();

        var query = new CalculatePriceQuery(customerId, queryStartDate, queryEndDate);

        var (response, _, statusCode) = await _handler.HandleAsync(query);

        Assert.Equal(HttpStatusCode.OK, statusCode);
        Assert.NotNull(response);
        Assert.Equal(expectedTotalPrice, response.TotalPrice, precision: 3);
    }

    // --- Complex Scenario Tests ---

    [Fact]
    public async Task HandleAsync_MultipleServicesAndDiscount_CalculatesCorrectTotal()
    {
        int customerId = 11;
        var queryStartDate = new DateTime(2024, 1, 1); // Monday
        var queryEndDate = new DateTime(2024, 1, 14);  // Sunday (2 full weeks)
        // Total 14 days.

        _dbContext.Customers.Add(new Customer { Id = customerId, GlobalFreeDays = 2 }); // 2 free days

        // Service A: 0.2/day, no weekends. Active Jan 1 - Jan 14.
        // Chargeable days: 10 weekdays (Jan 1-5, Jan 8-12)
        _dbContext.CustomerServiceUsages.Add(new CustomerServiceUsage { CustomerId = customerId, ServiceId = 1, StartDate = new DateTime(2024, 1, 1) });
        // Base Cost A: 10 * 0.2 = 2.0

        // Service C: 0.4/day, charges weekends. Active Jan 5 - Jan 14.
        // Chargeable days: 10 days (Jan 5-14)
        _dbContext.CustomerServiceUsages.Add(new CustomerServiceUsage { CustomerId = customerId, ServiceId = 3, StartDate = new DateTime(2024, 1, 5) });
        // Discount for Service C: 20% from Jan 8 - Jan 12
        _dbContext.Discounts.Add(new Discount
        {
            CustomerId = customerId,
            ServiceId = 3,
            Percentage = 0.20m,
            StartDate = new DateTime(2024, 1, 8),
            EndDate = new DateTime(2024, 1, 12)
        });
        // Cost C calculation:
        // Jan 5, 6, 7: Full price (3 days * 0.4 = 1.2)
        // Jan 8, 9, 10, 11, 12: Discounted (5 days * 0.4 * 0.8 = 1.6)
        // Jan 13, 14: Full price (2 days * 0.4 = 0.8)
        // Base Cost C: 1.2 + 1.6 + 0.8 = 3.6

        // Total Base Cost = 2.0 (A) + 3.6 (C) = 5.6
        // Total Chargeable Days (ignoring free days) = 10 (A) + 10 (C) = 20 (This isn't how free days work, they apply globally)
        // The calculation should apply free days to the most expensive days first.
        // Daily costs breakdown (ServiceA + ServiceC, considering discount):
        // Jan 1: 0.2 + 0   = 0.2
        // Jan 2: 0.2 + 0   = 0.2
        // Jan 3: 0.2 + 0   = 0.2
        // Jan 4: 0.2 + 0   = 0.2
        // Jan 5: 0.2 + 0.4 = 0.6  (Fri)
        // Jan 6: 0   + 0.4 = 0.4  (Sat)
        // Jan 7: 0   + 0.4 = 0.4  (Sun)
        // Jan 8: 0.2 + 0.32= 0.52 (Mon, C discounted)
        // Jan 9: 0.2 + 0.32= 0.52 (Tue, C discounted)
        // Jan 10:0.2 + 0.32= 0.52 (Wed, C discounted)
        // Jan 11:0.2 + 0.32= 0.52 (Thu, C discounted)
        // Jan 12:0.2 + 0.32= 0.52 (Fri, C discounted)
        // Jan 13:0   + 0.4 = 0.4  (Sat)
        // Jan 14:0   + 0.4 = 0.4  (Sun)
        // Sorted daily costs: [0.2, 0.2, 0.2, 0.2, 0.4, 0.4, 0.4, 0.4, 0.4, 0.52, 0.52, 0.52, 0.52, 0.52, 0.6]
        // Total sum = 5.6
        // Apply 2 free days to the CHEAPEST days (0.2, 0.2) based on code logic
        // Expected Total Price = 5.6 - 0.2 - 0.2 = 5.20
        var expectedTotalPrice = 5.20m;

        await _dbContext.SaveChangesAsync();

        var query = new CalculatePriceQuery(customerId, queryStartDate, queryEndDate);

        var (response, _, statusCode) = await _handler.HandleAsync(query);

        Assert.Equal(HttpStatusCode.OK, statusCode);
        Assert.NotNull(response);
        Assert.Equal(expectedTotalPrice, response.TotalPrice, precision: 3);
    }

}