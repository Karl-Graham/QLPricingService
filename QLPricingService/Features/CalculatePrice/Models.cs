namespace QLPricingService.Features.CalculatePrice;

/// <summary>
/// Represents the query parameters for calculating a price.
/// </summary>
/// <param name="CustomerId">The unique identifier for the customer.</param>
/// <param name="StartDate">The starting date (inclusive) for the calculation period.</param>
/// <param name="EndDate">The ending date (inclusive) for the calculation period.</param>
public record CalculatePriceQuery(
    int CustomerId,
    DateTime StartDate,
    DateTime EndDate
);

/// <summary>
/// Represents the response containing the calculated total price.
/// </summary>
/// <param name="TotalPrice">The final calculated price for the specified customer and period.</param>
public record CalculatePriceResponse(
    decimal TotalPrice
); 