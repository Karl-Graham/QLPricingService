namespace QLPricingService.Domain;

public class CustomerServiceUsage
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int ServiceId { get; set; }
    public DateTime StartDate { get; set; }
    public decimal? CustomerSpecificPricePerDay { get; set; } // Nullable, use Service.BasePrice if null

    // Navigation properties
    public Customer? Customer { get; set; }
    public Service? Service { get; set; }
} 