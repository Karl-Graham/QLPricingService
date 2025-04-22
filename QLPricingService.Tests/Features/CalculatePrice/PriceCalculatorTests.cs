using Xunit;
using QLPricingService.Domain;
using QLPricingService.Features.CalculatePrice;
using System;
using System.Collections.Generic;

namespace QLPricingService.Tests.Features.CalculatePrice;

public class PriceCalculatorTests
{
    private readonly Service _serviceA = new() { Id = 1, Name = "Service A", BasePricePerDay = 0.2m, ChargesOnWeekends = false };
    private readonly Service _serviceB = new() { Id = 2, Name = "Service B", BasePricePerDay = 0.24m, ChargesOnWeekends = false };
    private readonly Service _serviceC = new() { Id = 3, Name = "Service C", BasePricePerDay = 0.4m, ChargesOnWeekends = true };

    [Fact]
    public void CalculateTotalPrice_NoUsage_ReturnsZero()
    {
        var customer = new Customer { Id = 1, GlobalFreeDays = 0, ServiceUsages = new List<CustomerServiceUsage>(), Discounts = new List<Discount>() };
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 10);

        var totalPrice = PriceCalculator.CalculateTotalPrice(startDate, endDate, customer);

        Assert.Equal(0m, totalPrice);
    }

    [Fact]
    public void CalculateTotalPrice_ServiceNoWeekendCharge_SkipsWeekends()
    {
        var startDate = new DateTime(2024, 1, 1); // Monday
        var endDate = new DateTime(2024, 1, 7);   // Sunday (7 days total)
        var customer = new Customer
        {
            Id = 1,
            GlobalFreeDays = 0,
            Discounts = new List<Discount>(),
            ServiceUsages = new List<CustomerServiceUsage>
            {
                new() { CustomerId = 1, Service = _serviceA, ServiceId = _serviceA.Id, StartDate = startDate }
            }
        };
        var expectedPrice = 1.0m;

        var totalPrice = PriceCalculator.CalculateTotalPrice(startDate, endDate, customer);

        Assert.Equal(expectedPrice, totalPrice, precision: 2);
    }

    [Fact]
    public void CalculateTotalPrice_ServiceWithWeekendCharge_IncludesWeekends()
    {
        var startDate = new DateTime(2024, 1, 1); // Monday
        var endDate = new DateTime(2024, 1, 7);   // Sunday (7 days total)
        var customer = new Customer
        {
            Id = 1,
            GlobalFreeDays = 0,
            Discounts = new List<Discount>(),
            ServiceUsages = new List<CustomerServiceUsage>
            {
                new() { CustomerId = 1, Service = _serviceC, ServiceId = _serviceC.Id, StartDate = startDate }
            }
        };
        var expectedPrice = 2.8m;

        var totalPrice = PriceCalculator.CalculateTotalPrice(startDate, endDate, customer);

        Assert.Equal(expectedPrice, totalPrice, precision: 2);
    }

    [Fact]
    public void CalculateTotalPrice_WithDiscount_AppliesDiscountCorrectly()
    {
        var startDate = new DateTime(2024, 1, 1); // Monday
        var endDate = new DateTime(2024, 1, 10);  // Wednesday (10 days total)
        var customer = new Customer
        {
            Id = 1,
            GlobalFreeDays = 0,
            ServiceUsages = new List<CustomerServiceUsage>
            {
                new() { CustomerId = 1, Service = _serviceC, ServiceId = _serviceC.Id, StartDate = startDate }
            },
            Discounts = new List<Discount>
            {
                new() { CustomerId = 1, ServiceId = _serviceC.Id, Percentage = 0.25m, StartDate = new DateTime(2024, 1, 3), EndDate = new DateTime(2024, 1, 7) } // Wed-Sun
            }
        };
        var expectedPrice = 3.5m;

        var totalPrice = PriceCalculator.CalculateTotalPrice(startDate, endDate, customer);

        Assert.Equal(expectedPrice, totalPrice, precision: 2);
    }

    [Fact]
    public void CalculateTotalPrice_WithFreeDays_SkipsCheapestDays()
    {
        var startDate = new DateTime(2024, 1, 1); // Monday
        var endDate = new DateTime(2024, 1, 10);  // Wednesday (10 days total)
        var customer = new Customer
        {
            Id = 1,
            GlobalFreeDays = 3, // 3 Free days
            Discounts = new List<Discount>(),
            ServiceUsages = new List<CustomerServiceUsage>
            {
                new() { CustomerId = 1, Service = _serviceC, ServiceId = _serviceC.Id, StartDate = startDate }
            }
        };
        var expectedPrice = 2.8m;

        var totalPrice = PriceCalculator.CalculateTotalPrice(startDate, endDate, customer);

        Assert.Equal(expectedPrice, totalPrice, precision: 2);
    }

    [Fact]
    public void CalculateTotalPrice_OverlappingDiscounts_UsesHighestPercentage()
    {
        var startDate = new DateTime(2024, 1, 1); // Monday
        var endDate = new DateTime(2024, 1, 15);  // Monday (15 days total)
        var customer = new Customer
        {
            Id = 10,
            GlobalFreeDays = 0,
            ServiceUsages = new List<CustomerServiceUsage>
            {
                new() { CustomerId = 10, Service = _serviceC, ServiceId = _serviceC.Id, StartDate = startDate }
            },
            Discounts = new List<Discount>
            {
                new() { CustomerId = 10, ServiceId = _serviceC.Id, Percentage = 0.20m, StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 1, 10) }, // Lower % 
                new() { CustomerId = 10, ServiceId = _serviceC.Id, Percentage = 0.60m, StartDate = new DateTime(2024, 1, 5), EndDate = new DateTime(2024, 1, 15) }  // Higher %, overlaps
            }
        };
        var expectedPrice = 3.04m;

        var totalPrice = PriceCalculator.CalculateTotalPrice(startDate, endDate, customer);

        Assert.Equal(expectedPrice, totalPrice, precision: 2);
    }

    [Fact]
    public void CalculateTotalPrice_CustomerSpecificPrice_OverridesBasePrice()
    {
        var startDate = new DateTime(2024, 1, 1); // Monday
        var endDate = new DateTime(2024, 1, 10);  // Wednesday (10 days total)
        var customer = new Customer
        {
            Id = 11,
            GlobalFreeDays = 0,
            Discounts = new List<Discount>(), // No discount for this test
            ServiceUsages = new List<CustomerServiceUsage>
            {
                // Use specific price 0.50 instead of Service C base 0.4
                new() { CustomerId = 11, Service = _serviceC, ServiceId = _serviceC.Id, StartDate = startDate, CustomerSpecificPricePerDay = 0.50m }
            }
        };
        var expectedPrice = 5.00m;

        var totalPrice = PriceCalculator.CalculateTotalPrice(startDate, endDate, customer);

        Assert.Equal(expectedPrice, totalPrice, precision: 2);
    }

    [Fact]
    public void CalculateTotalPrice_FreeDaysEqualToChargeable_ReturnsZero()
    {
        var startDate = new DateTime(2024, 1, 1); // Monday
        var endDate = new DateTime(2024, 1, 7);   // Sunday (7 days total)
        var customer = new Customer
        {
            Id = 12,
            GlobalFreeDays = 5, // 5 Free days
            Discounts = new List<Discount>(),
            ServiceUsages = new List<CustomerServiceUsage>
            {
                new() { CustomerId = 12, Service = _serviceA, ServiceId = _serviceA.Id, StartDate = startDate } // Service A (no weekend charge)
            }
        };
        // Chargeable days: Mon-Fri = 5 days. Costs: 5 * 0.2 = 1.0
        // Free days = 5. Skip 5 cheapest days (all 0.2). Result = 0.
        var expectedPrice = 0m;

        var totalPrice = PriceCalculator.CalculateTotalPrice(startDate, endDate, customer);

        Assert.Equal(expectedPrice, totalPrice, precision: 2);
    }
}