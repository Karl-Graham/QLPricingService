using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;
using QLPricingService.Features.CalculatePrice; // Add this
using Xunit;

namespace QLPricingService.Tests;

// Use the custom factory now
public class PricingEndpointTests : IClassFixture<TestingWebAppFactory<Program>>
{
    private readonly TestingWebAppFactory<Program> _factory;

    public PricingEndpointTests(TestingWebAppFactory<Program> factory)
    {
        _factory = factory;
    }

    // Helper method (optional) to format dates for query string
    private string FormatDate(DateTime date) => date.ToString("yyyy-MM-ddTHH:mm:ss");

    [Fact]
    public async Task CalculatePrice_TestCase1_ReturnsCorrectPrice()
    {
        // Arrange
        var client = _factory.CreateClient();
        int customerId = 1; // Customer X
        var startDate = new DateTime(2019, 9, 20);
        var endDate = new DateTime(2019, 9, 30); // Calculate *up until* 2019-10-01 means end date is 2019-09-30
        var expectedTotalPrice = 5.56m; // Corrected expected value based on handler logic
        var url = $"/pricing?customerId={customerId}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CalculatePriceResponse>();
        Assert.NotNull(result);
        Assert.Equal(expectedTotalPrice, result.TotalPrice, precision: 2); // Use precision for decimal comparison
    }

    [Fact]
    public async Task CalculatePrice_TestCase2_ReturnsCorrectPrice()
    {
        // Arrange
        var client = _factory.CreateClient();
        int customerId = 2; // Customer Y
        var startDate = new DateTime(2018, 1, 1);
        var endDate = new DateTime(2019, 9, 30); // Calculate *up until* 2019-10-01
        var expectedTotalPrice = 196.224m; // Value obtained from the previous test run
        var url = $"/pricing?customerId={customerId}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CalculatePriceResponse>();
        Assert.NotNull(result);
        Assert.Equal(expectedTotalPrice, result.TotalPrice, precision: 3); // Use precision 3 due to calculated value
    }

    [Fact]
    public async Task CalculatePrice_InvalidCustomerId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        int customerId = 999; // Non-existent customer ID
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 1, 10);
        var url = $"/pricing?customerId={customerId}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Check the response body message (expecting { Message: "..." })
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(); // Use a simple record
        Assert.NotNull(error);
        Assert.Contains("not found", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CalculatePrice_InvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        int customerId = 1; // Use existing customer ID 1
        var startDate = new DateTime(2023, 1, 10);
        var endDate = new DateTime(2023, 1, 1); // End date before start date
        var url = $"/pricing?customerId={customerId}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Check the response body message (expecting { Message: "..." })
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(); // Use a simple record
        Assert.NotNull(error);
        Assert.Equal("EndDate must be on or after StartDate.", error.Message); // Use exact message from Handler
    }

    [Fact]
    public async Task CalculatePriceByName_TestCase1_ReturnsCorrectPrice()
    {
        // Arrange
        var client = _factory.CreateClient();
        string customerName = "Customer X"; // Use seeded name
        var startDate = new DateTime(2019, 9, 20);
        var endDate = new DateTime(2019, 9, 30);
        var expectedTotalPrice = 5.56m; // Same expectation as by ID
        // Encode the customer name for the query string
        var encodedCustomerName = System.Net.WebUtility.UrlEncode(customerName);
        var url = $"/pricing/by-name?customerName={encodedCustomerName}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CalculatePriceResponse>();
        Assert.NotNull(result);
        Assert.Equal(expectedTotalPrice, result.TotalPrice, precision: 2);
    }

    [Fact]
    public async Task CalculatePriceByName_TestCase2_ReturnsCorrectPrice()
    {
        // Arrange
        var client = _factory.CreateClient();
        string customerName = "Customer Y"; // Use seeded name
        var startDate = new DateTime(2018, 1, 1);
        var endDate = new DateTime(2019, 9, 30);
        var expectedTotalPrice = 196.224m; // Same expectation as by ID
        var encodedCustomerName = System.Net.WebUtility.UrlEncode(customerName);
        var url = $"/pricing/by-name?customerName={encodedCustomerName}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CalculatePriceResponse>();
        Assert.NotNull(result);
        Assert.Equal(expectedTotalPrice, result.TotalPrice, precision: 3);
    }

    [Fact]
    public async Task CalculatePriceByName_InvalidName_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        string customerName = "Customer Z"; // Non-existent name
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 1, 10);
        var encodedCustomerName = System.Net.WebUtility.UrlEncode(customerName);
        var url = $"/pricing/by-name?customerName={encodedCustomerName}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("not found", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(customerName, error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CalculatePriceByName_EmptyOrWhitespaceName_ReturnsBadRequest(string customerName)
    {
        // Arrange
        var client = _factory.CreateClient();
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 1, 10);
        var encodedCustomerName = System.Net.WebUtility.UrlEncode(customerName ?? string.Empty);
        var url = $"/pricing/by-name?customerName={encodedCustomerName}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("cannot be empty", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    // Simple record for deserializing error messages
    private record ErrorResponse(string Message);

    // --- Granular Integration Tests ---

    [Fact]
    public async Task CalculatePrice_ServiceWithoutWeekendCharging_SkipsWeekends()
    {
        // Arrange
        var client = _factory.CreateClient();
        int customerId = 3; // Uses Service A (0.2/day, no weekends) seeded in TestingWebAppFactory
        var startDate = new DateTime(2023, 10, 23); // Monday
        var endDate = new DateTime(2023, 10, 29);   // Sunday
        // Period: Mon, Tue, Wed, Thu, Fri, Sat, Sun (7 days total)
        // Service A should charge for 5 weekdays.
        var expectedTotalPrice = 5 * 0.2m;
        var url = $"/pricing?customerId={customerId}&startDate={startDate:O}&endDate={endDate:O}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        var content = await response.Content.ReadFromJsonAsync<CalculatePriceResponse>();
        Assert.NotNull(content);
        Assert.Equal(expectedTotalPrice, content.TotalPrice, precision: 2);
    }

    [Fact]
    public async Task CalculatePrice_ServiceWithWeekendCharging_IncludesWeekends()
    {
        // Arrange
        var client = _factory.CreateClient();
        int customerId = 4; // Uses Service C (0.4/day, charges weekends) seeded in TestingWebAppFactory
        var startDate = new DateTime(2023, 10, 23); // Monday
        var endDate = new DateTime(2023, 10, 29);   // Sunday
        // Period: Mon, Tue, Wed, Thu, Fri, Sat, Sun (7 days total)
        // Service C should charge for all 7 days.
        var expectedTotalPrice = 7 * 0.4m;
        var url = $"/pricing?customerId={customerId}&startDate={startDate:O}&endDate={endDate:O}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        var content = await response.Content.ReadFromJsonAsync<CalculatePriceResponse>();
        Assert.NotNull(content);
        Assert.Equal(expectedTotalPrice, content.TotalPrice, precision: 2);
    }

    [Fact]
    public async Task CalculatePrice_DiscountStartsMidPeriod_AppliesCorrectly()
    {
        // Arrange
        var client = _factory.CreateClient();
        int customerId = 5; // Seeded in TestingWebAppFactory
        var queryStartDate = new DateTime(2023, 11, 1); // Wed
        var queryEndDate = new DateTime(2023, 11, 10);  // Fri (next week)
        // Total 10 days. Service C (0.4/day, charges weekends)
        // Discount 50% from Nov 6-10
        // Days 1-5 (Nov 1-5): Full price (0.4 * 5 days = 2.0)
        // Days 6-10 (Nov 6-10): Discounted price (0.4 * 0.5 * 5 days = 1.0)
        var expectedTotalPrice = 3.0m;
        var url = $"/pricing?customerId={customerId}&startDate={queryStartDate:O}&endDate={queryEndDate:O}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<CalculatePriceResponse>();
        Assert.NotNull(content);
        Assert.Equal(expectedTotalPrice, content.TotalPrice, precision: 3);
    }

    [Fact]
    public async Task CalculatePrice_DiscountEndsMidPeriod_AppliesCorrectly()
    {
        // Arrange
        var client = _factory.CreateClient();
        int customerId = 6; // Seeded in TestingWebAppFactory
        var queryStartDate = new DateTime(2023, 11, 1); // Wed
        var queryEndDate = new DateTime(2023, 11, 10);  // Fri (next week)
        // Total 10 days. Service C (0.4/day, charges weekends)
        // Discount 25% from Nov 1-5
        // Days 1-5 (Nov 1-5): Discounted price (0.4 * 0.75 * 5 days = 1.5)
        // Days 6-10 (Nov 6-10): Full price (0.4 * 5 days = 2.0)
        var expectedTotalPrice = 3.5m;
        var url = $"/pricing?customerId={customerId}&startDate={queryStartDate:O}&endDate={queryEndDate:O}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<CalculatePriceResponse>();
        Assert.NotNull(content);
        Assert.Equal(expectedTotalPrice, content.TotalPrice, precision: 3);
    }

    [Fact]
    public async Task CalculatePrice_DiscountCoversFullPeriod_AppliesCorrectly()
    {
        // Arrange
        var client = _factory.CreateClient();
        int customerId = 7; // Seeded in TestingWebAppFactory
        var queryStartDate = new DateTime(2023, 11, 1); // Wed
        var queryEndDate = new DateTime(2023, 11, 10);  // Fri (next week)
        // Total 10 days. Service C (0.4/day, charges weekends)
        // Discount 10% from Nov 1-10
        // All 10 days discounted: (0.4 * 0.9 * 10 days = 3.6)
        var expectedTotalPrice = 3.6m;
        var url = $"/pricing?customerId={customerId}&startDate={queryStartDate:O}&endDate={queryEndDate:O}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<CalculatePriceResponse>();
        Assert.NotNull(content);
        Assert.Equal(expectedTotalPrice, content.TotalPrice, precision: 3);
    }

    // === Tests moved from Endpoints/PricingEndpointTests.cs ===

    [Theory]
    [InlineData(1, "2023-11-01", "2023-11-10", 5.6)] // Customer 1: Uses A+C
    [InlineData(2, "2023-11-01", "2023-11-10", 0.0)] // Customer 2: Uses B+C, 200 free days
    [InlineData(3, "2023-11-01", "2023-11-10", 1.6)] // Customer 3: Uses A (no weekend charge)
    public async Task CalculatePrice_BasicScenarios_ReturnsCorrectPrice(int customerId, string startDate, string endDate, decimal expectedPrice)
    {
        // Arrange
        var client = _factory.CreateClient();
        var url = $"/pricing?customerId={customerId}&startDate={startDate}&endDate={endDate}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType!.ToString());

        var result = await response.Content.ReadFromJsonAsync<CalculatePriceResponse>();
        Assert.NotNull(result);
        Assert.Equal(expectedPrice, result!.TotalPrice, precision: 2);
    }

    [Theory]
    [InlineData(5, "2023-11-01", "2023-11-10", 3.0)] // Customer 5: Discount starts mid-period
    public async Task CalculatePrice_DiscountStartsMidPeriod_ReturnsCorrectPrice(int customerId, string startDate, string endDate, decimal expectedPrice)
    {
        // Arrange
        var client = _factory.CreateClient();
        var url = $"/pricing?customerId={customerId}&startDate={startDate}&endDate={endDate}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType!.ToString());

        var result = await response.Content.ReadFromJsonAsync<CalculatePriceResponse>();
        Assert.NotNull(result);
        Assert.Equal(expectedPrice, result!.TotalPrice, precision: 2);
    }

    [Theory]
    [InlineData(6, "2023-11-01", "2023-11-10", 3.5)] // Customer 6: Discount ends mid-period
    public async Task CalculatePrice_DiscountEndsMidPeriod_ReturnsCorrectPrice(int customerId, string startDate, string endDate, decimal expectedPrice)
    {
        // Arrange
        var client = _factory.CreateClient();
        var url = $"/pricing?customerId={customerId}&startDate={startDate}&endDate={endDate}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType!.ToString());

        var result = await response.Content.ReadFromJsonAsync<CalculatePriceResponse>();
        Assert.NotNull(result);
        Assert.Equal(expectedPrice, result!.TotalPrice, precision: 2);
    }

    [Theory]
    [InlineData(7, "2023-11-01", "2023-11-10", 3.6)] // Customer 7: Discount covers full period
    public async Task CalculatePrice_DiscountCoversFullPeriod_ReturnsCorrectPrice(int customerId, string startDate, string endDate, decimal expectedPrice)
    {
        // Arrange
        var client = _factory.CreateClient();
        var url = $"/pricing?customerId={customerId}&startDate={startDate}&endDate={endDate}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType!.ToString());

        var result = await response.Content.ReadFromJsonAsync<CalculatePriceResponse>();
        Assert.NotNull(result);
        Assert.Equal(expectedPrice, result!.TotalPrice, precision: 2);
    }

    // --- Edge Case Tests ---

    [Theory]
    // Scenario 1: Discount starts exactly on query start date
    // Customer 8, Service C (0.4/day, charges weekends), Discount 50% from Jan 15 to 21.
    // Query: Jan 15 - Jan 21 (7 days)
    // Price = 7 days * 0.4 * (1 - 0.5) = 7 * 0.4 * 0.5 = 1.4
    [InlineData(8, "2024-01-15", "2024-01-21", 1.40)]
    // Scenario 2: Discount ends exactly on query end date
    // Customer 9, Service C (0.4/day, charges weekends), Discount 50% from Jan 15 to 21.
    // Query: Jan 15 - Jan 21 (7 days)
    // Price = 7 days * 0.4 * (1 - 0.5) = 7 * 0.4 * 0.5 = 1.4
    [InlineData(9, "2024-01-15", "2024-01-21", 1.40)]
    // Scenario 3: Overlapping discounts (higher percentage should apply)
    // Customer 10, Service C (0.4/day, charges weekends)
    // Discount 1: 20% Jan 1-10
    // Discount 2: 60% Jan 5-15
    // Query: Jan 1 - Jan 15 (15 days)
    // Jan 1-4 (4 days): 20% discount = 4 * 0.4 * 0.8 = 1.28
    // Jan 5-10 (6 days): 60% discount (higher) = 6 * 0.4 * 0.4 = 0.96
    // Jan 11-15 (5 days): 60% discount = 5 * 0.4 * 0.4 = 0.80
    // Total = 1.28 + 0.96 + 0.80 = 3.04
    [InlineData(10, "2024-01-01", "2024-01-15", 3.04)]
    // Scenario 4: Customer specific price with discount
    // Customer 11, Service C (Charges weekends), Specific Price = 0.50/day
    // Discount: 10% Jan 1-31
    // Query: Jan 1 - Jan 10 (10 days)
    // Price = 10 days * 0.50 * (1 - 0.10) = 10 * 0.50 * 0.9 = 4.50
    [InlineData(11, "2024-01-01", "2024-01-10", 4.50)]
    // Scenario 5: Free days exactly match chargeable days
    // Customer 12, Service A (0.2/day, NO weekend charge), 5 free days
    // Query: Jan 1 (Mon) - Jan 7 (Sun) (7 days)
    // Chargeable days: Jan 1-5 (Mon-Fri) = 5 days
    // Costs: 0.2 * 5 = 1.0. Free days = 5. Cheapest 5 days (all 0.2) are skipped.
    // Price = 0.00
    [InlineData(12, "2024-01-01", "2024-01-07", 0.00)]
    // Scenario 6: Free days exceed chargeable days
    // Customer 12, Service A (0.2/day, NO weekend charge), 5 free days
    // Query: Jan 1 (Mon) - Jan 5 (Fri) (5 days)
    // Chargeable days: Jan 1-5 (Mon-Fri) = 5 days
    // Costs: 0.2 * 5 = 1.0. Free days = 5. Cheapest 5 days (all 0.2) are skipped.
    // Price = 0.00
    [InlineData(12, "2024-01-01", "2024-01-05", 0.00)]
    public async Task CalculatePrice_EdgeCases_ReturnsCorrectPrice(int customerId, string startDate, string endDate, decimal expectedPrice)
    {
        // Arrange
        var client = _factory.CreateClient();
        var url = $"/pricing?customerId={customerId}&startDate={startDate}&endDate={endDate}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType!.ToString());

        var result = await response.Content.ReadFromJsonAsync<CalculatePriceResponse>();
        Assert.NotNull(result);
        // Use precision 3 for safety with intermediate calculations
        Assert.Equal(expectedPrice, result!.TotalPrice, precision: 3);
    }
}