using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QLPricingService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    GlobalFreeDays = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    BasePricePerDay = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    ChargesOnWeekends = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerServiceUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CustomerSpecificPricePerDay = table.Column<decimal>(type: "decimal(18, 4)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerServiceUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerServiceUsages_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerServiceUsages_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Discounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5, 4)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Discounts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Discounts_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "GlobalFreeDays", "Name" },
                values: new object[,]
                {
                    { 1, 0, "Customer X" },
                    { 2, 200, "Customer Y" }
                });

            migrationBuilder.InsertData(
                table: "Services",
                columns: new[] { "Id", "BasePricePerDay", "ChargesOnWeekends", "Name" },
                values: new object[,]
                {
                    { 1, 0.2m, false, "Service A" },
                    { 2, 0.24m, false, "Service B" },
                    { 3, 0.4m, true, "Service C" }
                });

            migrationBuilder.InsertData(
                table: "CustomerServiceUsages",
                columns: new[] { "Id", "CustomerId", "CustomerSpecificPricePerDay", "ServiceId", "StartDate" },
                values: new object[,]
                {
                    { 1, 1, null, 1, new DateTime(2019, 9, 20, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, 1, null, 3, new DateTime(2019, 9, 20, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, 2, null, 2, new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, 2, null, 3, new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "Discounts",
                columns: new[] { "Id", "CustomerId", "EndDate", "Percentage", "ServiceId", "StartDate" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2019, 9, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), 0.20m, 3, new DateTime(2019, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, 2, new DateTime(2099, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), 0.30m, 2, new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, 2, new DateTime(2099, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), 0.30m, 3, new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceUsages_CustomerId",
                table: "CustomerServiceUsages",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceUsages_ServiceId",
                table: "CustomerServiceUsages",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_CustomerId",
                table: "Discounts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_ServiceId",
                table: "Discounts",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerServiceUsages");

            migrationBuilder.DropTable(
                name: "Discounts");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Services");
        }
    }
}
