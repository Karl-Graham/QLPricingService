namespace QLPricingService.Domain;

public class Discount
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int ServiceId { get; set; } // Foreign key to Service
    public decimal Percentage { get; set; } // E.g., 0.20 for 20%
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Navigation properties
    public Customer? Customer { get; set; }
    public Service? Service { get; set; }
} 