using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QLPricingService.Data; // Namespace for PricingDbContext
using QLPricingService.Domain; // Namespace for domain entities
using System.Data.Common; // For DbConnection
using Microsoft.Extensions.Logging; // Add this using statement

namespace QLPricingService.Tests;

public class TestingWebAppFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the app's DbContext registration.
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PricingDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Create open SqliteConnection so EF Core doesn't automatically close it.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Add DbContext using the in-memory SQLite database for testing.
            services.AddDbContext<PricingDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Build the service provider to create a scope for seeding.
            // REMOVED Seeding logic from here - will be done in CreateHost override
            /*
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<PricingDbContext>();
                var logger = scopedServices.GetRequiredService<ILogger<TestingWebAppFactory<TEntryPoint>>>();

                try 
                {
                    // Ensure DB is created (includes HasData seeding)
                    db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();
                    logger.LogInformation("Database created.");

                    // Clear tables seeded by HasData to avoid conflicts
                    db.Customers.RemoveRange(db.Customers);
                    db.CustomerServiceUsages.RemoveRange(db.CustomerServiceUsages);
                    db.Discounts.RemoveRange(db.Discounts);
                    db.SaveChanges(); // Commit removals before adding test data
                    logger.LogInformation("Existing seed data cleared.");

                    // --- Seed Test-Specific Data --- 
                    // Customers (1-7)
                    db.Customers.AddRange(
                        new Customer { Id = 1, Name = "Customer X", GlobalFreeDays = 0 },
                        new Customer { Id = 2, Name = "Customer Y", GlobalFreeDays = 200 },
                        new Customer { Id = 3, Name = "Weekend Test User A", GlobalFreeDays = 0 },
                        new Customer { Id = 4, Name = "Weekend Test User C", GlobalFreeDays = 0 },
                        new Customer { Id = 5, Name = "Discount Mid Start User", GlobalFreeDays = 0 },
                        new Customer { Id = 6, Name = "Discount Mid End User", GlobalFreeDays = 0 },
                        new Customer { Id = 7, Name = "Discount Full Period User", GlobalFreeDays = 0 }
                    );

                    // Customer Service Usages (for 1-7) 
                    db.CustomerServiceUsages.AddRange(
                        new CustomerServiceUsage { CustomerId = 1, ServiceId = 1, StartDate = new DateTime(2019, 9, 20) },
                        new CustomerServiceUsage { CustomerId = 1, ServiceId = 3, StartDate = new DateTime(2019, 9, 20) },
                        new CustomerServiceUsage { CustomerId = 2, ServiceId = 2, StartDate = new DateTime(2018, 1, 1) },
                        new CustomerServiceUsage { CustomerId = 2, ServiceId = 3, StartDate = new DateTime(2018, 1, 1) },
                        new CustomerServiceUsage { CustomerId = 3, ServiceId = 1, StartDate = new DateTime(2023, 1, 1) }, 
                        new CustomerServiceUsage { CustomerId = 4, ServiceId = 3, StartDate = new DateTime(2023, 1, 1) },  
                        new CustomerServiceUsage { CustomerId = 5, ServiceId = 3, StartDate = new DateTime(2023, 1, 1) }, 
                        new CustomerServiceUsage { CustomerId = 6, ServiceId = 3, StartDate = new DateTime(2023, 1, 1) }, 
                        new CustomerServiceUsage { CustomerId = 7, ServiceId = 3, StartDate = new DateTime(2023, 1, 1) }  
                    );

                    // Discounts (for 1, 2, 5, 6, 7)
                    db.Discounts.AddRange(
                        new Discount { CustomerId = 1, ServiceId = 3, Percentage = 0.20m, StartDate = new DateTime(2019, 9, 22), EndDate = new DateTime(2019, 9, 24) },
                        new Discount { CustomerId = 2, ServiceId = 2, Percentage = 0.30m, StartDate = new DateTime(2018, 1, 1), EndDate = new DateTime(2099, 12, 31) },
                        new Discount { CustomerId = 2, ServiceId = 3, Percentage = 0.30m, StartDate = new DateTime(2018, 1, 1), EndDate = new DateTime(2099, 12, 31) },
                        new Discount { CustomerId = 5, ServiceId = 3, Percentage = 0.50m, StartDate = new DateTime(2023, 11, 6), EndDate = new DateTime(2023, 11, 10) },
                        new Discount { CustomerId = 6, ServiceId = 3, Percentage = 0.25m, StartDate = new DateTime(2023, 11, 1), EndDate = new DateTime(2023, 11, 5) },
                        new Discount { CustomerId = 7, ServiceId = 3, Percentage = 0.10m, StartDate = new DateTime(2023, 11, 1), EndDate = new DateTime(2023, 11, 10) }
                    );

                    // Save all test data
                    db.SaveChanges();
                    logger.LogInformation("In-memory database seeded with test data.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the database for testing.");
                    throw;
                }
            }
            */
        });

        builder.UseEnvironment("Development");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Perform seeding after the host is built, using its scope
        using (var scope = host.Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<PricingDbContext>();
            var logger = scopedServices.GetRequiredService<ILogger<TestingWebAppFactory<TEntryPoint>>>();

            try
            {
                // Ensure DB is created (includes HasData seeding)
                // Note: EnsureDeleted might not be strictly necessary here if the DB is always fresh,
                // but it guarantees a clean slate if the factory instance were reused unexpectedly.
                db.Database.EnsureDeleted(); 
                db.Database.EnsureCreated();
                logger.LogInformation("Database created by CreateHost override.");

                // Clear tables potentially seeded by HasData to avoid conflicts
                db.Customers.RemoveRange(db.Customers);
                db.CustomerServiceUsages.RemoveRange(db.CustomerServiceUsages);
                db.Discounts.RemoveRange(db.Discounts);
                db.SaveChanges(); // Commit removals before adding test data
                logger.LogInformation("Existing seed data cleared by CreateHost override.");

                // --- Seed Test-Specific Data --- 
                // Customers (1-7)
                 db.Customers.AddRange(
                    new Customer { Id = 1, Name = "Customer X", GlobalFreeDays = 0 },
                    new Customer { Id = 2, Name = "Customer Y", GlobalFreeDays = 200 },
                    new Customer { Id = 3, Name = "Weekend Test User A", GlobalFreeDays = 0 },
                    new Customer { Id = 4, Name = "Weekend Test User C", GlobalFreeDays = 0 },
                    new Customer { Id = 5, Name = "Discount Mid Start User", GlobalFreeDays = 0 },
                    new Customer { Id = 6, Name = "Discount Mid End User", GlobalFreeDays = 0 },
                    new Customer { Id = 7, Name = "Discount Full Period User", GlobalFreeDays = 0 },
                    // New Customers for Edge Case Tests (8-12)
                    new Customer { Id = 8, Name = "Edge: Discount Start Match", GlobalFreeDays = 0 },
                    new Customer { Id = 9, Name = "Edge: Discount End Match", GlobalFreeDays = 0 },
                    new Customer { Id = 10, Name = "Edge: Overlapping Discounts", GlobalFreeDays = 0 },
                    new Customer { Id = 11, Name = "Edge: Specific Price + Discount", GlobalFreeDays = 0 },
                    new Customer { Id = 12, Name = "Edge: Free Days Match/Exceed", GlobalFreeDays = 5 }
                );

                // Services (A, B, C)
                db.Services.AddRange(
                    new Service { Id = 1, Name = "Service A", BasePricePerDay = 0.2m, ChargesOnWeekends = false },
                    new Service { Id = 2, Name = "Service B", BasePricePerDay = 0.24m, ChargesOnWeekends = false },
                    new Service { Id = 3, Name = "Service C", BasePricePerDay = 0.4m, ChargesOnWeekends = true }
                 );

                // Customer Service Usages (for 1-7) 
                db.CustomerServiceUsages.AddRange(
                    new CustomerServiceUsage { CustomerId = 1, ServiceId = 1, StartDate = new DateTime(2019, 9, 20) },
                    new CustomerServiceUsage { CustomerId = 1, ServiceId = 3, StartDate = new DateTime(2019, 9, 20) },
                    new CustomerServiceUsage { CustomerId = 2, ServiceId = 2, StartDate = new DateTime(2018, 1, 1) },
                    new CustomerServiceUsage { CustomerId = 2, ServiceId = 3, StartDate = new DateTime(2018, 1, 1) },
                    new CustomerServiceUsage { CustomerId = 3, ServiceId = 1, StartDate = new DateTime(2023, 1, 1) }, 
                    new CustomerServiceUsage { CustomerId = 4, ServiceId = 3, StartDate = new DateTime(2023, 1, 1) },  
                    new CustomerServiceUsage { CustomerId = 5, ServiceId = 3, StartDate = new DateTime(2023, 1, 1) }, 
                    new CustomerServiceUsage { CustomerId = 6, ServiceId = 3, StartDate = new DateTime(2023, 1, 1) }, 
                    new CustomerServiceUsage { CustomerId = 7, ServiceId = 3, StartDate = new DateTime(2023, 1, 1) },
                    // New Usages for Edge Cases (Customers 8-12)
                    new CustomerServiceUsage { CustomerId = 8, ServiceId = 3, StartDate = new DateTime(2024, 1, 1) }, // Service C (Charges weekends)
                    new CustomerServiceUsage { CustomerId = 9, ServiceId = 3, StartDate = new DateTime(2024, 1, 1) }, // Service C
                    new CustomerServiceUsage { CustomerId = 10, ServiceId = 3, StartDate = new DateTime(2024, 1, 1) },// Service C
                    new CustomerServiceUsage { CustomerId = 11, ServiceId = 3, StartDate = new DateTime(2024, 1, 1), CustomerSpecificPricePerDay = 0.50m }, // Service C, specific price
                    new CustomerServiceUsage { CustomerId = 12, ServiceId = 1, StartDate = new DateTime(2024, 1, 1) }  // Service A (No weekend charge)
                );

                // Discounts (for 1, 2, 5, 6, 7)
                 db.Discounts.AddRange(
                    new Discount { CustomerId = 1, ServiceId = 3, Percentage = 0.20m, StartDate = new DateTime(2019, 9, 22), EndDate = new DateTime(2019, 9, 24) },
                    new Discount { CustomerId = 2, ServiceId = 2, Percentage = 0.30m, StartDate = new DateTime(2018, 1, 1), EndDate = new DateTime(2099, 12, 31) },
                    new Discount { CustomerId = 2, ServiceId = 3, Percentage = 0.30m, StartDate = new DateTime(2018, 1, 1), EndDate = new DateTime(2099, 12, 31) },
                    new Discount { CustomerId = 5, ServiceId = 3, Percentage = 0.50m, StartDate = new DateTime(2023, 11, 6), EndDate = new DateTime(2023, 11, 10) },
                    new Discount { CustomerId = 6, ServiceId = 3, Percentage = 0.25m, StartDate = new DateTime(2023, 11, 1), EndDate = new DateTime(2023, 11, 5) },
                    new Discount { CustomerId = 7, ServiceId = 3, Percentage = 0.10m, StartDate = new DateTime(2023, 11, 1), EndDate = new DateTime(2023, 11, 10) },
                    // New Discounts for Edge Cases (Customers 8-12)
                    new Discount { CustomerId = 8, ServiceId = 3, Percentage = 0.50m, StartDate = new DateTime(2024, 1, 15), EndDate = new DateTime(2024, 1, 21) }, // Starts Mon 15th
                    new Discount { CustomerId = 9, ServiceId = 3, Percentage = 0.50m, StartDate = new DateTime(2024, 1, 15), EndDate = new DateTime(2024, 1, 21) }, // Ends Sun 21st
                    new Discount { CustomerId = 10, ServiceId = 3, Percentage = 0.20m, StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 1, 10) }, // Lower % 
                    new Discount { CustomerId = 10, ServiceId = 3, Percentage = 0.60m, StartDate = new DateTime(2024, 1, 5), EndDate = new DateTime(2024, 1, 15) }, // Higher %, overlaps
                    new Discount { CustomerId = 11, ServiceId = 3, Percentage = 0.10m, StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 1, 31) } // Discount on specific price
                );

                // Save all test data
                db.SaveChanges();
                logger.LogInformation("In-memory database seeded by CreateHost override.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred seeding the database in CreateHost override.");
                throw;
            }
        }

        return host;
    }

    // Dispose the connection when the factory is disposed
    protected override void Dispose(bool disposing)
    {
        if (_connection != null)
        {
            _connection.Close();
            _connection.Dispose();
            _connection = null;
        }
        base.Dispose(disposing);
    }
} 