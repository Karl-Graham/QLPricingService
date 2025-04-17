using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QLPricingService.Data; // Namespace for PricingDbContext
using QLPricingService.Domain; // Namespace for domain entities
using System.Data.Common; // For DbConnection

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
            var sp = services.BuildServiceProvider();

            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<PricingDbContext>();
                var logger = scopedServices.GetRequiredService<ILogger<TestingWebAppFactory<TEntryPoint>>>();

                db.Database.EnsureCreated();

                try
                {
                    SeedDatabase(db);
                    logger.LogInformation("In-memory database seeded for test run.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the database for testing.");
                    throw;
                }
            }
        });

        builder.UseEnvironment("Development");
    }

    private void SeedDatabase(PricingDbContext context)
    {
        // Seed data consistent with DbContext HasData, but ensures it runs for tests
        // In a real app, consider if DbContext seed is sufficient or if tests need different/more data
        
        // Note: Services are already seeded via HasData in DbContext, EnsureCreated handles this.

        // Customers (Match IDs used in DbContext HasData)
        context.Customers.AddRange(
            new Customer { Id = 1, Name = "Customer X", GlobalFreeDays = 0 },
            new Customer { Id = 2, Name = "Customer Y", GlobalFreeDays = 200 }
        );
        context.SaveChanges(); // Save customers before adding related data

        // Customer Service Usages (Use IDs from DbContext HasData for consistency if needed, or let DB generate)
        context.CustomerServiceUsages.AddRange(
            // Customer X
            new CustomerServiceUsage { Id = 1, CustomerId = 1, ServiceId = 1, StartDate = new DateTime(2019, 9, 20) },
            new CustomerServiceUsage { Id = 2, CustomerId = 1, ServiceId = 3, StartDate = new DateTime(2019, 9, 20) },
            // Customer Y
            new CustomerServiceUsage { Id = 3, CustomerId = 2, ServiceId = 2, StartDate = new DateTime(2018, 1, 1) },
            new CustomerServiceUsage { Id = 4, CustomerId = 2, ServiceId = 3, StartDate = new DateTime(2018, 1, 1) }
        );

        // Discounts (Use IDs from DbContext HasData for consistency if needed, or let DB generate)
        context.Discounts.AddRange(
            // Customer X
            new Discount
            {
                Id = 1,
                CustomerId = 1,
                ServiceId = 3, // Discount for Service C
                Percentage = 0.20m, 
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
        
        // Remove Test Case 2 discount logic explanation (moved to README/Design Notes)
        // context.SaveChanges(); // Save changes after adding usages/discounts
        // Using AddRange + single SaveChanges is slightly more efficient
        try
        {
             context.SaveChanges();
        }
        catch(DbUpdateException ex)
        {
             // Log or inspect ex.Entries to see what caused the issue
             Console.WriteLine($"Error saving seed data: {ex.InnerException?.Message ?? ex.Message}");
             throw;
        }
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