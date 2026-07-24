using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TMDT1_TH.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingDeliveryModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedDeliveryAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingCarrierName",
                table: "Orders",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingNote",
                table: "Orders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShippingServiceId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingServiceName",
                table: "Orders",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingNumber",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingUrl",
                table: "Orders",
                type: "nvarchar(700)",
                maxLength: 700,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ShippingCarriers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrackingUrlTemplate = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingCarriers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShippingServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShippingCarrierId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BaseFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedMinDays = table.Column<int>(type: "int", nullable: false),
                    EstimatedMaxDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingServices", x => x.Id);
                    table.CheckConstraint("CK_ShippingServices_BaseFee", "[BaseFee] >= 0");
                    table.CheckConstraint("CK_ShippingServices_EstimatedDays", "[EstimatedMinDays] >= 0 AND [EstimatedMaxDays] >= [EstimatedMinDays]");
                    table.ForeignKey(
                        name: "FK_ShippingServices_ShippingCarriers_ShippingCarrierId",
                        column: x => x.ShippingCarrierId,
                        principalTable: "ShippingCarriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ShippingCarriers",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DisplayOrder", "IsActive", "Name", "PhoneNumber", "TrackingUrlTemplate", "UpdatedAt", "UpdatedBy", "WebsiteUrl" },
                values: new object[] { 1, "MAYHOME", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", 1, true, "Mây Home Delivery", null, null, null, null, null });

            migrationBuilder.InsertData(
                table: "ShippingServices",
                columns: new[] { "Id", "BaseFee", "Code", "CreatedAt", "CreatedBy", "Description", "DisplayOrder", "EstimatedMaxDays", "EstimatedMinDays", "IsActive", "Name", "ShippingCarrierId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, 30000m, "STANDARD", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", "Phù hợp với đơn hàng thông thường.", 1, 5, 3, true, "Giao hàng tiêu chuẩn", 1, null, null },
                    { 2, 50000m, "EXPRESS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", "Ưu tiên xử lý và giao trong thời gian ngắn.", 2, 2, 1, true, "Giao hàng nhanh", 1, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShippingServiceId",
                table: "Orders",
                column: "ShippingServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TrackingNumber",
                table: "Orders",
                column: "TrackingNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingCarriers_Code",
                table: "ShippingCarriers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShippingCarriers_IsActive_DisplayOrder",
                table: "ShippingCarriers",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingServices_IsActive_DisplayOrder",
                table: "ShippingServices",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingServices_ShippingCarrierId_Code",
                table: "ShippingServices",
                columns: new[] { "ShippingCarrierId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ShippingServices_ShippingServiceId",
                table: "Orders",
                column: "ShippingServiceId",
                principalTable: "ShippingServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ShippingServices_ShippingServiceId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "ShippingServices");

            migrationBuilder.DropTable(
                name: "ShippingCarriers");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ShippingServiceId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TrackingNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EstimatedDeliveryAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingCarrierName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingNote",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingServiceId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingServiceName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TrackingNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TrackingUrl",
                table: "Orders");
        }
    }
}
