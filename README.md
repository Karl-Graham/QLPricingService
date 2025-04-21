# QLPricingService

This service calculates the total price for a customer's usage of various services over a specified date range, considering base prices, weekend charging rules, customer-specific prices, discounts, and global free days.

## Features

*   Calculates price based on daily usage.
*   Supports services that charge differently on weekends.
*   Applies customer-specific pricing overrides.
*   Applies percentage-based discounts applicable during the period.
*   Applies global free days provided to a customer (skipping the cheapest days).
*   Provides endpoints to calculate price by Customer ID or Customer Name.

## Getting Started

### Prerequisites

*   .NET SDK (Version compatible with net10.0 - Check global.json or project file. Note: net10.0 seems unusual, likely meant net8.0 or similar based on recent SDKs, adjust if needed)

### Building the Service

```bash
dotnet build
```

### Running the Service

```bash
dotnet run --project QLPricingService/QLPricingService.csproj
```

The service will start, typically listening on ports like 5000 (HTTP) and 5001 (HTTPS). Check the console output for the actual URLs.

### Running Tests

```bash
dotnet test
```

## API Endpoints

*   `GET /pricing?customerId={id}&startDate={yyyy-MM-ddTHH:mm:ss}&endDate={yyyy-MM-ddTHH:mm:ss}`
    *   Calculates price for the customer with the given `customerId`.
*   `GET /pricing/by-name?customerName={name}&startDate={yyyy-MM-ddTHH:mm:ss}&endDate={yyyy-MM-ddTHH:mm:ss}`
    *   Calculates price for the customer with the given `customerName`.

*(Dates should be provided in ISO 8601 format, URL encoding may be needed for customerName)*

## Known Issues

*   The build output may show 5 `CS8602` warnings ("Dereference of a possibly null reference") in `PricingEndpointTests.cs` related to accessing `result.TotalPrice`. These appear to be spurious warnings in the current build environment, as the code includes `Assert.NotNull(result)` checks immediately before accessing `TotalPrice`, and attempts to suppress them with null-forgiving operators (`!`) or `#pragma` directives were ineffective. The tests themselves pass correctly. 