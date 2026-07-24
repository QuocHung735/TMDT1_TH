using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDT1_TH.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionModule_20260725031819 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PromotionCode",
                table: "Orders",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromotionName",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Promotions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaximumDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MinimumOrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UsageLimit = table.Column<int>(type: "int", nullable: true),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.Id);
                    table.CheckConstraint("CK_Promotions_Amounts", "[MinimumOrderAmount] >= 0 AND ([MaximumDiscountAmount] IS NULL OR [MaximumDiscountAmount] > 0)");
                    table.CheckConstraint("CK_Promotions_DiscountValue", "[DiscountValue] > 0");
                    table.CheckConstraint("CK_Promotions_Percentage", "[DiscountType] <> 1 OR ([DiscountValue] > 0 AND [DiscountValue] <= 100)");
                    table.CheckConstraint("CK_Promotions_Period", "[EndsAt] > [StartsAt]");
                    table.CheckConstraint("CK_Promotions_Usage", "[UsedCount] >= 0 AND ([UsageLimit] IS NULL OR ([UsageLimit] > 0 AND [UsedCount] <= [UsageLimit]))");
                });

            migrationBuilder.CreateTable(
                name: "PromotionMarkets",
                columns: table => new
                {
                    PromotionId = table.Column<int>(type: "int", nullable: false),
                    MarketId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionMarkets", x => new { x.PromotionId, x.MarketId });
                    table.ForeignKey(
                        name: "FK_PromotionMarkets_Markets_MarketId",
                        column: x => x.MarketId,
                        principalTable: "Markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionMarkets_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PromotionCode",
                table: "Orders",
                column: "PromotionCode");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionMarkets_MarketId",
                table: "PromotionMarkets",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Code",
                table: "Promotions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_IsActive_StartsAt_EndsAt",
                table: "Promotions",
                columns: new[] { "IsActive", "StartsAt", "EndsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromotionMarkets");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PromotionCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PromotionCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PromotionName",
                table: "Orders");
        }
    }
}
