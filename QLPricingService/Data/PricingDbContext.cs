using Microsoft.EntityFrameworkCore;
using QLPricingService.Domain;

namespace QLPricingService.Data;

public class PricingDbContext : DbContext
{
    public PricingDbContext(DbContextOptions<PricingDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<CustomerServiceUsage> CustomerServiceUsages { get; set; }
    public DbSet<Discount> Discounts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // === Configurations ===
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(c => c.Name).IsRequired();
            entity.HasMany(c => c.ServiceUsages).WithOne(u => u.Customer).HasForeignKey(u => u.CustomerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(c => c.Discounts).WithOne(d => d.Customer).HasForeignKey(d => d.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Service>(entity => 
        {
            entity.Property(s => s.BasePricePerDay).HasColumnType("decimal(18, 4)");
            entity.HasMany(s => s.CustomerUsages).WithOne(u => u.Service).HasForeignKey(u => u.ServiceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(s => s.Discounts).WithOne(d => d.Service).HasForeignKey(d => d.ServiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerServiceUsage>(entity => 
        {
            entity.Property(u => u.CustomerSpecificPricePerDay).HasColumnType("decimal(18, 4)");
        });

        modelBuilder.Entity<Discount>(entity => 
        {
            entity.Property(d => d.Percentage).HasColumnType("decimal(5, 4)");
        });

        // === Seed Data ===
        // (Keep comments explaining which customer/scenario is being seeded)
        modelBuilder.Entity<Service>().HasData(
            new Service { Id = 1, Name = "Service A", BasePricePerDay = 0.2m, ChargesOnWeekends = false },
            new Service { Id = 2, Name = "Service B", BasePricePerDay = 0.24m, ChargesOnWeekends = false },
            new Service { Id = 3, Name = "Service C", BasePricePerDay = 0.4m, ChargesOnWeekends = true }
        );

        modelBuilder.Entity<Customer>().HasData(
            new Customer { Id = 1, Name = "Customer X", GlobalFreeDays = 0 },
            new Customer { Id = 2, Name = "Customer Y", GlobalFreeDays = 200 }
        );

        modelBuilder.Entity<CustomerServiceUsage>().HasData(
            // Customer X
            new CustomerServiceUsage { Id = 1, CustomerId = 1, ServiceId = 1, StartDate = new DateTime(2019, 9, 20) },
            new CustomerServiceUsage { Id = 2, CustomerId = 1, ServiceId = 3, StartDate = new DateTime(2019, 9, 20) },
            // Customer Y
            new CustomerServiceUsage { Id = 3, CustomerId = 2, ServiceId = 2, StartDate = new DateTime(2018, 1, 1) },
            new CustomerServiceUsage { Id = 4, CustomerId = 2, ServiceId = 3, StartDate = new DateTime(2018, 1, 1) }
        );

        modelBuilder.Entity<Discount>().HasData(
            // Customer X
            new Discount
            {
                Id = 1,
                CustomerId = 1,
                ServiceId = 3, // Discount for Service C
                Percentage = 0.20m, // 20%
                StartDate = new DateTime(2019, 9, 22),
                EndDate = new DateTime(2019, 9, 24)
            },
            // Customer Y
            new Discount // 30% discount for Service B
            {
                Id = 2,
                CustomerId = 2,
                ServiceId = 2,
                Percentage = 0.30m,
                StartDate = new DateTime(2018, 1, 1),
                EndDate = new DateTime(2099, 12, 31) // Far future end date
            },
            new Discount // 30% discount for Service C
            {
                Id = 3,
                CustomerId = 2,
                ServiceId = 3,
                Percentage = 0.30m,
                StartDate = new DateTime(2018, 1, 1),
                EndDate = new DateTime(2099, 12, 31) // Far future end date
            }
        );
    }
} 