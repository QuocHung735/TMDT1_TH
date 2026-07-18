using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TMDT1_TH.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommerceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Markets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Markets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PriceScheduleId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    ProductVariantId = table.Column<int>(type: "int", nullable: true),
                    MarketId = table.Column<int>(type: "int", nullable: false),
                    OldCostPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NewCostPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OldListPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NewListPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OldSalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NewSalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OldValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NewValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OldValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NewValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    BrandId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(280)", maxLength: 280, nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    HasVariants = table.Column<bool>(type: "bit", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Products_StockQuantity", "[StockQuantity] >= 0");
                    table.CheckConstraint("CK_Products_Weight", "[Weight] IS NULL OR [Weight] >= 0");
                    table.ForeignKey(
                        name: "FK_Products_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductOptions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CombinationKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.CheckConstraint("CK_ProductVariants_StockQuantity", "[StockQuantity] >= 0");
                    table.CheckConstraint("CK_ProductVariants_Weight", "[Weight] IS NULL OR [Weight] >= 0");
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductOptionValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductOptionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ColorCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOptionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductOptionValues_ProductOptions_ProductOptionId",
                        column: x => x.ProductOptionId,
                        principalTable: "ProductOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    ProductVariantId = table.Column<int>(type: "int", nullable: true),
                    MarketId = table.Column<int>(type: "int", nullable: false),
                    CostPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ListPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceSchedules", x => x.Id);
                    table.CheckConstraint("CK_PriceSchedules_Dates", "[ValidTo] IS NULL OR [ValidTo] > [ValidFrom]");
                    table.CheckConstraint("CK_PriceSchedules_Prices", "[CostPrice] >= 0 AND [ListPrice] > 0 AND [SalePrice] > 0 AND [SalePrice] <= [ListPrice]");
                    table.CheckConstraint("CK_PriceSchedules_Target", "([ProductId] IS NOT NULL AND [ProductVariantId] IS NULL) OR ([ProductId] IS NULL AND [ProductVariantId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PriceSchedules_Markets_MarketId",
                        column: x => x.MarketId,
                        principalTable: "Markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PriceSchedules_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PriceSchedules_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantValues",
                columns: table => new
                {
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    ProductOptionValueId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantValues", x => new { x.ProductVariantId, x.ProductOptionValueId });
                    table.ForeignKey(
                        name: "FK_ProductVariantValues_ProductOptionValues_ProductOptionValueId",
                        column: x => x.ProductOptionValueId,
                        principalTable: "ProductOptionValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductVariantValues_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Markets",
                columns: new[] { "Id", "Code", "CountryCode", "CreatedAt", "CreatedBy", "CurrencyCode", "Description", "IsActive", "IsDefault", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, "ONLINE", "VN", new DateTime(2026, 7, 18, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", "VND", "Giá áp dụng cho website và kênh bán trực tuyến.", true, true, "Kênh trực tuyến", null, null },
                    { 2, "VN-HCM", "VN", new DateTime(2026, 7, 18, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", "VND", null, true, false, "Thành phố Hồ Chí Minh", null, null },
                    { 3, "VN-HN", "VN", new DateTime(2026, 7, 18, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", "VND", null, true, false, "Hà Nội", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Slug",
                table: "Brands",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId_DisplayOrder",
                table: "Categories",
                columns: new[] { "ParentId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Markets_Code",
                table: "Markets",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Markets_IsDefault",
                table: "Markets",
                column: "IsDefault",
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_MarketId_ChangedAt",
                table: "PriceHistories",
                columns: new[] { "MarketId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_PriceScheduleId_ChangedAt",
                table: "PriceHistories",
                columns: new[] { "PriceScheduleId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_ProductId_ChangedAt",
                table: "PriceHistories",
                columns: new[] { "ProductId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_ProductVariantId_ChangedAt",
                table: "PriceHistories",
                columns: new[] { "ProductVariantId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSchedules_MarketId_IsActive_ValidFrom_ValidTo",
                table: "PriceSchedules",
                columns: new[] { "MarketId", "IsActive", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSchedules_ProductId_MarketId_ValidFrom",
                table: "PriceSchedules",
                columns: new[] { "ProductId", "MarketId", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSchedules_ProductVariantId_MarketId_ValidFrom",
                table: "PriceSchedules",
                columns: new[] { "ProductVariantId", "MarketId", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_DisplayOrder",
                table: "ProductImages",
                columns: new[] { "ProductId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductVariantId",
                table: "ProductImages",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_ProductId_Name",
                table: "ProductOptions",
                columns: new[] { "ProductId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionValues_ProductOptionId_Value",
                table: "ProductOptionValues",
                columns: new[] { "ProductOptionId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_BrandId_Status",
                table: "Products",
                columns: new[] { "BrandId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId_Status",
                table: "Products",
                columns: new[] { "CategoryId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                table: "Products",
                column: "Sku",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Barcode",
                table: "ProductVariants",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId",
                unique: true,
                filter: "[IsDefault] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_CombinationKey",
                table: "ProductVariants",
                columns: new[] { "ProductId", "CombinationKey" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Sku",
                table: "ProductVariants",
                column: "Sku",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantValues_ProductOptionValueId",
                table: "ProductVariantValues",
                column: "ProductOptionValueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceHistories");

            migrationBuilder.DropTable(
                name: "PriceSchedules");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "ProductVariantValues");

            migrationBuilder.DropTable(
                name: "Markets");

            migrationBuilder.DropTable(
                name: "ProductOptionValues");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "ProductOptions");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
