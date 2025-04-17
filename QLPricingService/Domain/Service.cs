namespace QLPricingService.Domain;

public class Service
{
    public int Id { get; set; } // Could be an enum if services are fixed
    public string Name { get; set; } = string.Empty;
    public decimal BasePricePerDay { get; set; }
    public bool ChargesOnWeekends { get; set; }

    // Navigation properties
    public ICollection<CustomerServiceUsage> CustomerUsages { get; set; } = new List<CustomerServiceUsage>();
    public ICollection<Discount> Discounts { get; set; } = new List<Discount>();
} 