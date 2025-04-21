using QLPricingService.Domain;

namespace QLPricingService.Features.CalculatePrice;

/// <summary>
/// Encapsulates the logic for calculating prices based on customer data and usage periods.
/// </summary>
public static class PriceCalculator
{
    public static decimal CalculateTotalPrice(DateTime startDate, DateTime endDate, Customer customer)
    {
        var dailyCosts = CalculateAllDailyCosts(startDate, endDate, customer);
        var totalPrice = ApplyFreeDays(dailyCosts, customer.GlobalFreeDays);
        return totalPrice;
    }

    private static List<decimal> CalculateAllDailyCosts(DateTime startDate, DateTime endDate, Customer customer)
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

    private static decimal CalculateSingleDayCost(DateTime currentDate, Customer customer)
    {
        decimal currentDayTotalCost = 0m;
        DayOfWeek currentDayOfWeek = currentDate.DayOfWeek;

        var activeUsagesOnDate = customer.ServiceUsages
            .Where(u => u.StartDate.Date <= currentDate)
            .ToList();

        foreach (var usage in activeUsagesOnDate)
        {
            // Ensure Service is loaded - this check is important as we are outside the EF context query now
            if (usage.Service == null)
            {
                // Log or handle this scenario appropriately - perhaps throw an exception
                // For now, we'll skip this usage if the service isn't loaded, 
                // but this indicates a potential issue with data loading upstream.
                // Consider throwing InvalidOperationException if Service navigation property is expected to be loaded.
                continue; 
            }

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

    private static decimal GetDiscountMultiplier(DateTime currentDate, int serviceId, IEnumerable<Discount> discounts)
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

    private static decimal ApplyFreeDays(List<decimal> dailyCosts, int freeDays)
    {
        // Ensure the list is not null
        if (dailyCosts == null)
        {
            return 0m; // Or throw ArgumentNullException based on desired behavior
        }
        
        // Sort daily costs low-to-high to remove the cheapest N days
        dailyCosts.Sort();
        int freeDaysToApply = Math.Min(freeDays, dailyCosts.Count);
        // Use Skip and Sum for potentially better performance on large lists
        return dailyCosts.Skip(freeDaysToApply).Sum();
    }
} 