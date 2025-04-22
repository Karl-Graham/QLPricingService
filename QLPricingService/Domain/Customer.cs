namespace QLPricingService.Domain;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int GlobalFreeDays { get; set; }

    // Navigation properties (optional but helpful)
    public ICollection<CustomerServiceUsage> ServiceUsages { get; set; } = new List<CustomerServiceUsage>();
    public ICollection<Discount> Discounts { get; set; } = new List<Discount>();
}