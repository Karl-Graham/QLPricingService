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
            var dailyCosts = CalculateAllDailyCosts(query.StartDate, query.EndDate, customer);
            var totalPrice = ApplyFreeDays(dailyCosts, customer.GlobalFreeDays);

            _logger.LogInformation("Calculated total price for Customer {CustomerId} is {TotalPrice}", customer.Id, totalPrice);
            return (new CalculatePriceResponse(totalPrice), null, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during price calculation for Customer {CustomerId}", query.CustomerId);
            return (null, "An error occurred during price calculation.", HttpStatusCode.InternalServerError);
        }
    }

    private (CalculatePriceResponse? Response, string? ErrorMessage, HttpStatusCode StatusCode)? ValidateQuery(CalculatePriceQuery query)
    {
        // Basic date range validation
        if (query.EndDate < query.StartDate)
        {
            _logger.LogWarning("Invalid date range provided: StartDate {StartDate} is after EndDate {EndDate}",
                query.StartDate, query.EndDate);
            return (null, "End date cannot be earlier than start date.", HttpStatusCode.BadRequest);
        }
        return null; // No error
    }

    private async Task<Customer?> FetchCustomerDataAsync(int customerId, CancellationToken cancellationToken)
    {
        // Eager load related data needed for calculation
        return await _dbContext.Customers
            .Include(c => c.ServiceUsages)
                .ThenInclude(u => u.Service)
            .Include(c => c.Discounts)
                .ThenInclude(d => d.Service)
            .AsSplitQuery() // Optimization for multiple collection Includes
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
    }

    private List<decimal> CalculateAllDailyCosts(DateTime startDate, DateTime endDate, Customer customer)
    {
        var dailyCosts = new List<decimal>();
        for (var currentDate = startDate.Date; currentDate <= endDate.Date; currentDate = currentDate.AddDays(1))
        {
            decimal currentDayTotalCost = CalculateSingleDayCost(currentDate, customer);
            // Only include days that actually incurred a cost
            if (currentDayTotalCost > 0)
            {
                dailyCosts.Add(currentDayTotalCost);
            }
        }
        return dailyCosts;
    }

    private decimal CalculateSingleDayCost(DateTime currentDate, Customer customer)
    {
        decimal currentDayTotalCost = 0m;
        DayOfWeek currentDayOfWeek = currentDate.DayOfWeek;

        var activeUsagesOnDate = customer.ServiceUsages
            .Where(u => u.StartDate.Date <= currentDate)
            .ToList(); 

        foreach (var usage in activeUsagesOnDate)
        {
            if (usage.Service == null) continue; // Should not happen with Includes, but defensive check

            bool isChargeableDay = usage.Service.ChargesOnWeekends ||
                                   (currentDayOfWeek != DayOfWeek.Saturday && currentDayOfWeek != DayOfWeek.Sunday);

            if (isChargeableDay)
            {
                decimal basePrice = usage.CustomerSpecificPricePerDay ?? usage.Service.BasePricePerDay;
                decimal discountMultiplier = GetDiscountMultiplier(currentDate, usage.ServiceId, customer.Discounts);
                decimal dailyServicePrice = basePrice * discountMultiplier;
                currentDayTotalCost += dailyServicePrice;
            }
        }
        return currentDayTotalCost;
    }

    private decimal GetDiscountMultiplier(DateTime currentDate, int serviceId, IEnumerable<Discount> discounts)
    {
        // Find the best applicable discount for the service on the given date
        var applicableDiscount = discounts
            .Where(d => d.ServiceId == serviceId &&
                        d.StartDate.Date <= currentDate &&
                        d.EndDate.Date >= currentDate)
            .OrderByDescending(d => d.Percentage) // Apply highest discount if overlapping
            .FirstOrDefault();

        return applicableDiscount != null ? (1.0m - applicableDiscount.Percentage) : 1.0m;
    }

    private decimal ApplyFreeDays(List<decimal> dailyCosts, int freeDays)
    {
        // Sort daily costs low-to-high and skip the cheapest N days
        dailyCosts.Sort();
        int freeDaysToApply = Math.Min(freeDays, dailyCosts.Count);
        return dailyCosts.Skip(freeDaysToApply).Sum();
    }
} 