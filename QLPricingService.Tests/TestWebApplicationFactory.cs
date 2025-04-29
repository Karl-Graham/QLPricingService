using Microsoft.AspNetCore.Mvc.Testing;
using System.Reflection;

namespace QLPricingService.Tests
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        static TestWebApplicationFactory()
        {
            // This is a placeholder for bypassing deps file check
            // Implementation removed since it wasn't working
        }

        private static void NoOpEnsureDepsFile()
        {
            // Bypass the check - no implementation needed
        }
    }
} 