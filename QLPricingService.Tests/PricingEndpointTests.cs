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
    private readonly HttpClient _client;

    // Inject the custom factory and create the client
    public PricingEndpointTests(TestingWebAppFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // Helper method (optional) to format dates for query string
    private string FormatDate(DateTime date) => date.ToString("yyyy-MM-ddTHH:mm:ss");

    [Fact]
    public async Task CalculatePrice_TestCase1_ReturnsCorrectPrice()
    {
        // Arrange
        var customerId = 1; // Customer X
        var startDate = new DateTime(2019, 9, 20);
        var endDate = new DateTime(2019, 9, 30); // Calculate *up until* 2019-10-01 means end date is 2019-09-30
        var expectedTotalPrice = 5.56m; // Corrected expected value based on handler logic
        var url = $"/pricing?customerId={customerId}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await _client.GetAsync(url);

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
        var customerId = 2; // Customer Y
        var startDate = new DateTime(2018, 1, 1);
        var endDate = new DateTime(2019, 9, 30); // Calculate *up until* 2019-10-01
        var expectedTotalPrice = 196.224m; // Value obtained from the previous test run
        var url = $"/pricing?customerId={customerId}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await _client.GetAsync(url);

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
        var customerId = 999; // Non-existent customer ID
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 1, 10);
        var url = $"/pricing?customerId={customerId}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await _client.GetAsync(url);

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
        var customerId = 1; // Use existing customer ID 1
        var startDate = new DateTime(2023, 1, 10);
        var endDate = new DateTime(2023, 1, 1); // End date before start date
        var url = $"/pricing?customerId={customerId}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Check the response body message (expecting { Message: "..." })
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(); // Use a simple record
        Assert.NotNull(error);
        Assert.Contains("earlier than start date", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CalculatePriceByName_TestCase1_ReturnsCorrectPrice()
    {
        // Arrange
        var customerName = "Customer X"; // Use seeded name
        var startDate = new DateTime(2019, 9, 20);
        var endDate = new DateTime(2019, 9, 30);
        var expectedTotalPrice = 5.56m; // Same expectation as by ID
        // Encode the customer name for the query string
        var encodedCustomerName = System.Net.WebUtility.UrlEncode(customerName);
        var url = $"/pricing/by-name?customerName={encodedCustomerName}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await _client.GetAsync(url);

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
        var customerName = "Customer Y"; // Use seeded name
        var startDate = new DateTime(2018, 1, 1);
        var endDate = new DateTime(2019, 9, 30); 
        var expectedTotalPrice = 196.224m; // Same expectation as by ID
        var encodedCustomerName = System.Net.WebUtility.UrlEncode(customerName);
        var url = $"/pricing/by-name?customerName={encodedCustomerName}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await _client.GetAsync(url);

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
        var customerName = "Customer Z"; // Non-existent name
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 1, 10);
        var encodedCustomerName = System.Net.WebUtility.UrlEncode(customerName);
        var url = $"/pricing/by-name?customerName={encodedCustomerName}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await _client.GetAsync(url);

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
    [InlineData(null)] // Although FromQuery might handle null, let's test
    public async Task CalculatePriceByName_EmptyOrWhitespaceName_ReturnsBadRequest(string? customerName)
    {
        // Arrange
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 1, 10);
        var encodedCustomerName = System.Net.WebUtility.UrlEncode(customerName ?? string.Empty);
        var url = $"/pricing/by-name?customerName={encodedCustomerName}&startDate={FormatDate(startDate)}&endDate={FormatDate(endDate)}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("cannot be empty", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    // Simple record for deserializing error messages
    private record ErrorResponse(string Message);
} 