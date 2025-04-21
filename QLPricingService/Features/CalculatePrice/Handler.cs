using Microsoft.EntityFrameworkCore;
using QLPricingService.Data;
using QLPricingService.Domain;
using System.Net; // Add this for HttpStatusCode

namespace QLPricingService.Features.CalculatePrice;

public class Handler
{
    private readonly PricingDbContext _dbContext;
    private readonly ILogger<Handler> _logger; // Add logger

    // Inject ILogger
    public Handler(PricingDbContext dbContext, ILogger<Handler> logger) 
    {
        _dbContext = dbContext;
        _logger = logger; // Store logger
    }

    // Return a tuple indicating success/failure and status code
    public async Task<(CalculatePriceResponse? Response, string? ErrorMessage, HttpStatusCode StatusCode)> HandleAsync(
        CalculatePriceQuery query, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling CalculatePrice query for Customer {CustomerId} from {StartDate} to {EndDate}", 
            query.CustomerId, query.StartDate, query.EndDate);

        // Reinstate manual validation call as automatic validation is not triggering reliably for GET
        var validationResult = ValidateQuery(query);
        if (validationResult is not null) return validationResult.Value;

        Customer? customer;
        try
        {
            customer = await FetchCustomerDataAsync(query.CustomerId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer {CustomerId} from database", query.CustomerId);
            return (null, "An error occurred while fetching customer data.", HttpStatusCode.InternalServerError);
        }

        if (customer == null)
        {
            _logger.LogWarning("Customer with ID {CustomerId} not found", query.CustomerId);
            return (null, $"Customer with ID {query.CustomerId} not found.", HttpStatusCode.NotFound);
        }

        _logger.LogInformation("Customer {CustomerId} found with {FreeDays} free days", customer.Id, customer.GlobalFreeDays);

        try
        {
            // Use the dedicated calculator class
            var totalPrice = PriceCalculator.CalculateTotalPrice(query.StartDate, query.EndDate, customer);

            _logger.LogInformation("Calculated total price for Customer {CustomerId} is {TotalPrice}", customer.Id, totalPrice);
            return (new CalculatePriceResponse(totalPrice), null, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during price calculation for Customer {CustomerId}", query.CustomerId);
            return (null, "An error occurred during price calculation.", HttpStatusCode.InternalServerError);
        }
    }

    // Reinstate the validation method
    private (CalculatePriceResponse? Response, string? ErrorMessage, HttpStatusCode StatusCode)? ValidateQuery(CalculatePriceQuery query)
    {
        // Basic date range validation
        if (query.EndDate < query.StartDate)
        {
            _logger.LogWarning("Invalid date range provided: StartDate {StartDate} is after EndDate {EndDate}",
                query.StartDate, query.EndDate);
            // Use the message from the validator for consistency
            return (null, "EndDate must be on or after StartDate.", HttpStatusCode.BadRequest); 
        }
        return null; // No error
    }

    private async Task<Customer?> FetchCustomerDataAsync(int customerId, CancellationToken cancellationToken)
    {
        // Eager load related data needed for calculation
        return await _dbContext.Customers
            .Include(c => c.ServiceUsages)
                .ThenInclude(u => u.Service) // Ensure Service is included for PriceCalculator
            .Include(c => c.Discounts)
                .ThenInclude(d => d.Service) // Also include Service here if needed by discount logic later
            .AsSplitQuery() // Optimization for multiple collection Includes
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
    }
} 