# QL Pricing Service

This project implements a .NET Minimal Web API service responsible for calculating prices based on customer usage, service type, discounts, and free days.

## Features

*   Calculates prices for services A, B, and C based on daily/working day rates.
*   Supports customer-specific pricing.
*   Applies time-based percentage discounts per customer per service.
*   Accounts for global free days for customers.
*   Uses EF Core with SQLite for data persistence.
*   Built with Vertical Slice Architecture concepts.
*   Includes integration tests for specified scenarios.

## Prerequisites

*   .NET SDK (Version compatible with the project - e.g., .NET 8, .NET 9/10 Preview)

## Setup & Running

1.  **Clone the repository.**
2.  **Navigate to the `QLPricingService` directory:**
    ```bash
    cd QLPricingService
    ```
3.  **Restore dependencies:**
    ```bash
    dotnet restore
    ```
4.  **Apply Database Migrations:** (This creates the `pricing.db` file if it doesn't exist)
    ```bash
    dotnet ef database update
    ```
5.  **Run the application:**
    ```bash
    dotnet run
    ```
    The API will be available at `https://localhost:<port>` or `http://localhost:<port>` (check console output for the exact port).

## API Endpoint

*   **GET /pricing**
    *   Calculates the total price for a customer over a given period.
    *   **Query Parameters:**
        *   `customerId` (integer, required): The ID of the customer.
        *   `startDate` (datetime, required): The start date of the period (ISO 8601 format, e.g., `2023-01-01T00:00:00`).
        *   `endDate` (datetime, required): The end date of the period (inclusive, ISO 8601 format, e.g., `2023-01-31T23:59:59`).
    *   **Responses:**
        *   `200 OK`: Returns `{ "totalPrice": decimal }`.
        *   `400 Bad Request`: If the input is invalid (e.g., end date before start date). Returns `{ "message": "Error description" }`.
        *   `404 Not Found`: If the `customerId` does not exist. Returns `{ "message": "Error description" }`.
        *   `500 Internal Server Error`: If an unexpected error occurs during processing.

## Running Tests

1.  **Navigate to the test project directory:**
    ```bash
    cd ../QLPricingService.Tests 
    ```
    (Assuming you are in the `QLPricingService` directory)
2.  **Run tests:**
    ```bash
    dotnet test
    ```

## Design Notes & Assumptions

*   Vertical Slice Architecture is used, grouping code by feature (`Features/CalculatePrice`).
*   EF Core with SQLite is used for simplicity. In-memory database is used for integration tests.
*   "Working day" is assumed to be Monday-Friday.
*   Discounts are applied daily to the relevant service cost *before* summing the daily total.
*   Free days are applied at the end by removing the N cheapest *daily total costs* (where N is the number of free days).
*   Dates in API requests are expected in a format parseable by `DateTime` (e.g., ISO 8601).
*   The end date in the query is inclusive. 