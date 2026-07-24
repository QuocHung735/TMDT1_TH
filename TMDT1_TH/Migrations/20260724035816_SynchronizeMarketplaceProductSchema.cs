using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDT1_TH.Migrations
{
    public partial class SynchronizeMarketplaceProductSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[Products]', N'U') IS NULL
    THROW 51020, N'Không tìm thấy bảng Products. Hãy chạy migration khởi tạo database trước.', 1;

IF OBJECT_ID(N'[dbo].[ProductVariants]', N'U') IS NULL
    THROW 51021, N'Không tìm thấy bảng ProductVariants. Hãy chạy migration khởi tạo database trước.', 1;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.Products', N'ModelNumber') IS NULL
    ALTER TABLE [dbo].[Products] ADD [ModelNumber] nvarchar(100) NULL;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.Products', N'Unit') IS NULL
    ALTER TABLE [dbo].[Products] ADD [Unit] nvarchar(50) NOT NULL
        CONSTRAINT [DF_Products_Unit] DEFAULT (N'Cái') WITH VALUES;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.Products', N'CountryOfOrigin') IS NULL
    ALTER TABLE [dbo].[Products] ADD [CountryOfOrigin] nvarchar(100) NULL;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.Products', N'ManufacturerName') IS NULL
    ALTER TABLE [dbo].[Products] ADD [ManufacturerName] nvarchar(250) NULL;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.Products', N'ManufacturerAddress') IS NULL
    ALTER TABLE [dbo].[Products] ADD [ManufacturerAddress] nvarchar(500) NULL;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.Products', N'WarrantyMonths') IS NULL
    ALTER TABLE [dbo].[Products] ADD [WarrantyMonths] int NULL;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.Products', N'LowStockThreshold') IS NULL
    ALTER TABLE [dbo].[Products] ADD [LowStockThreshold] int NOT NULL
        CONSTRAINT [DF_Products_LowStockThreshold] DEFAULT (5) WITH VALUES;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.Products', N'MinPurchaseQuantity') IS NULL
    ALTER TABLE [dbo].[Products] ADD [MinPurchaseQuantity] int NOT NULL
        CONSTRAINT [DF_Products_MinPurchaseQuantity] DEFAULT (1) WITH VALUES;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.Products', N'MaxPurchaseQuantity') IS NULL
    ALTER TABLE [dbo].[Products] ADD [MaxPurchaseQuantity] int NULL;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.Products', N'PackageLengthCm') IS NULL
    ALTER TABLE [dbo].[Products] ADD [PackageLengthCm] decimal(10, 2) NULL;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.Products', N'PackageWidthCm') IS NULL
    ALTER TABLE [dbo].[Products] ADD [PackageWidthCm] decimal(10, 2) NULL;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.Products', N'PackageHeightCm') IS NULL
    ALTER TABLE [dbo].[Products] ADD [PackageHeightCm] decimal(10, 2) NULL;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.ProductVariants', N'LowStockThreshold') IS NULL
    ALTER TABLE [dbo].[ProductVariants] ADD [LowStockThreshold] int NOT NULL
        CONSTRAINT [DF_ProductVariants_LowStockThreshold] DEFAULT (5) WITH VALUES;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.ProductVariants', N'SortOrder') IS NULL
    ALTER TABLE [dbo].[ProductVariants] ADD [SortOrder] int NOT NULL
        CONSTRAINT [DF_ProductVariants_SortOrder] DEFAULT (0) WITH VALUES;
""");

            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[ProductSpecifications]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductSpecifications]
    (
        [Id] int IDENTITY(1,1) NOT NULL,
        [ProductId] int NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Value] nvarchar(1000) NOT NULL,
        [DisplayOrder] int NOT NULL CONSTRAINT [DF_ProductSpecifications_DisplayOrder] DEFAULT (0),
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ProductSpecifications_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        [CreatedBy] nvarchar(150) NULL,
        [UpdatedBy] nvarchar(150) NULL,
        CONSTRAINT [PK_ProductSpecifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductSpecifications_Products_ProductId]
            FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE
    );
END;
""");

            migrationBuilder.Sql("""
UPDATE [dbo].[Products] SET [LowStockThreshold] = 0 WHERE [LowStockThreshold] < 0;
UPDATE [dbo].[Products] SET [MinPurchaseQuantity] = 1 WHERE [MinPurchaseQuantity] < 1;
UPDATE [dbo].[Products]
SET [MaxPurchaseQuantity] = [MinPurchaseQuantity]
WHERE [MaxPurchaseQuantity] IS NOT NULL AND [MaxPurchaseQuantity] < [MinPurchaseQuantity];
UPDATE [dbo].[Products] SET [WarrantyMonths] = NULL WHERE [WarrantyMonths] < 0;
UPDATE [dbo].[Products] SET [PackageLengthCm] = NULL WHERE [PackageLengthCm] < 0;
UPDATE [dbo].[Products] SET [PackageWidthCm] = NULL WHERE [PackageWidthCm] < 0;
UPDATE [dbo].[Products] SET [PackageHeightCm] = NULL WHERE [PackageHeightCm] < 0;
UPDATE [dbo].[ProductVariants] SET [LowStockThreshold] = 0 WHERE [LowStockThreshold] < 0;
UPDATE [dbo].[ProductVariants] SET [SortOrder] = 0 WHERE [SortOrder] < 0;
""");

            migrationBuilder.Sql("""
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_ProductSpecifications_ProductId_DisplayOrder'
      AND [object_id] = OBJECT_ID(N'[dbo].[ProductSpecifications]')
)
    CREATE INDEX [IX_ProductSpecifications_ProductId_DisplayOrder]
        ON [dbo].[ProductSpecifications]([ProductId], [DisplayOrder]);
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Products_LowStockThreshold')
    ALTER TABLE [dbo].[Products] WITH CHECK
        ADD CONSTRAINT [CK_Products_LowStockThreshold] CHECK ([LowStockThreshold] >= 0);
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Products_PurchaseQuantity')
    ALTER TABLE [dbo].[Products] WITH CHECK
        ADD CONSTRAINT [CK_Products_PurchaseQuantity]
        CHECK ([MinPurchaseQuantity] >= 1
           AND ([MaxPurchaseQuantity] IS NULL OR [MaxPurchaseQuantity] >= [MinPurchaseQuantity]));
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Products_PackageDimensions')
    ALTER TABLE [dbo].[Products] WITH CHECK
        ADD CONSTRAINT [CK_Products_PackageDimensions]
        CHECK (([PackageLengthCm] IS NULL OR [PackageLengthCm] >= 0)
           AND ([PackageWidthCm] IS NULL OR [PackageWidthCm] >= 0)
           AND ([PackageHeightCm] IS NULL OR [PackageHeightCm] >= 0));
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Products_WarrantyMonths')
    ALTER TABLE [dbo].[Products] WITH CHECK
        ADD CONSTRAINT [CK_Products_WarrantyMonths]
        CHECK ([WarrantyMonths] IS NULL OR [WarrantyMonths] >= 0);
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_ProductVariants_LowStockThreshold')
    ALTER TABLE [dbo].[ProductVariants] WITH CHECK
        ADD CONSTRAINT [CK_ProductVariants_LowStockThreshold] CHECK ([LowStockThreshold] >= 0);
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_ProductVariants_SortOrder')
    ALTER TABLE [dbo].[ProductVariants] WITH CHECK
        ADD CONSTRAINT [CK_ProductVariants_SortOrder] CHECK ([SortOrder] >= 0);
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.Products', N'ModelNumber') IS NULL
 OR COL_LENGTH(N'dbo.Products', N'Unit') IS NULL
 OR COL_LENGTH(N'dbo.Products', N'CountryOfOrigin') IS NULL
 OR COL_LENGTH(N'dbo.Products', N'ManufacturerName') IS NULL
 OR COL_LENGTH(N'dbo.Products', N'ManufacturerAddress') IS NULL
 OR COL_LENGTH(N'dbo.Products', N'WarrantyMonths') IS NULL
 OR COL_LENGTH(N'dbo.Products', N'LowStockThreshold') IS NULL
 OR COL_LENGTH(N'dbo.Products', N'MinPurchaseQuantity') IS NULL
 OR COL_LENGTH(N'dbo.Products', N'MaxPurchaseQuantity') IS NULL
 OR COL_LENGTH(N'dbo.Products', N'PackageLengthCm') IS NULL
 OR COL_LENGTH(N'dbo.Products', N'PackageWidthCm') IS NULL
 OR COL_LENGTH(N'dbo.Products', N'PackageHeightCm') IS NULL
 OR COL_LENGTH(N'dbo.ProductVariants', N'LowStockThreshold') IS NULL
 OR COL_LENGTH(N'dbo.ProductVariants', N'SortOrder') IS NULL
 OR OBJECT_ID(N'[dbo].[ProductSpecifications]', N'U') IS NULL
    THROW 51022, N'Schema module sản phẩm chưa được nâng cấp đầy đủ.', 1;
""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_ProductVariants_SortOrder')
    ALTER TABLE [dbo].[ProductVariants] DROP CONSTRAINT [CK_ProductVariants_SortOrder];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_ProductVariants_LowStockThreshold')
    ALTER TABLE [dbo].[ProductVariants] DROP CONSTRAINT [CK_ProductVariants_LowStockThreshold];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Products_WarrantyMonths')
    ALTER TABLE [dbo].[Products] DROP CONSTRAINT [CK_Products_WarrantyMonths];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Products_PackageDimensions')
    ALTER TABLE [dbo].[Products] DROP CONSTRAINT [CK_Products_PackageDimensions];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Products_PurchaseQuantity')
    ALTER TABLE [dbo].[Products] DROP CONSTRAINT [CK_Products_PurchaseQuantity];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Products_LowStockThreshold')
    ALTER TABLE [dbo].[Products] DROP CONSTRAINT [CK_Products_LowStockThreshold];

IF OBJECT_ID(N'[dbo].[ProductSpecifications]', N'U') IS NOT NULL
    DROP TABLE [dbo].[ProductSpecifications];
""");

            migrationBuilder.Sql("""
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(dc.parent_object_id)) + N'.' +
               QUOTENAME(OBJECT_NAME(dc.parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
FROM sys.default_constraints dc
JOIN sys.columns c ON c.default_object_id = dc.object_id
WHERE dc.parent_object_id IN (OBJECT_ID(N'dbo.Products'), OBJECT_ID(N'dbo.ProductVariants'))
  AND c.name IN
  (
      N'Unit', N'LowStockThreshold', N'MinPurchaseQuantity', N'SortOrder'
  );
IF LEN(@sql) > 0 EXEC sys.sp_executesql @sql;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.ProductVariants', N'SortOrder') IS NOT NULL
    ALTER TABLE [dbo].[ProductVariants] DROP COLUMN [SortOrder];
IF COL_LENGTH(N'dbo.ProductVariants', N'LowStockThreshold') IS NOT NULL
    ALTER TABLE [dbo].[ProductVariants] DROP COLUMN [LowStockThreshold];

IF COL_LENGTH(N'dbo.Products', N'PackageHeightCm') IS NOT NULL
    ALTER TABLE [dbo].[Products] DROP COLUMN [PackageHeightCm];
IF COL_LENGTH(N'dbo.Products', N'PackageWidthCm') IS NOT NULL
    ALTER TABLE [dbo].[Products] DROP COLUMN [PackageWidthCm];
IF COL_LENGTH(N'dbo.Products', N'PackageLengthCm') IS NOT NULL
    ALTER TABLE [dbo].[Products] DROP COLUMN [PackageLengthCm];
IF COL_LENGTH(N'dbo.Products', N'MaxPurchaseQuantity') IS NOT NULL
    ALTER TABLE [dbo].[Products] DROP COLUMN [MaxPurchaseQuantity];
IF COL_LENGTH(N'dbo.Products', N'MinPurchaseQuantity') IS NOT NULL
    ALTER TABLE [dbo].[Products] DROP COLUMN [MinPurchaseQuantity];
IF COL_LENGTH(N'dbo.Products', N'LowStockThreshold') IS NOT NULL
    ALTER TABLE [dbo].[Products] DROP COLUMN [LowStockThreshold];
IF COL_LENGTH(N'dbo.Products', N'WarrantyMonths') IS NOT NULL
    ALTER TABLE [dbo].[Products] DROP COLUMN [WarrantyMonths];
IF COL_LENGTH(N'dbo.Products', N'ManufacturerAddress') IS NOT NULL
    ALTER TABLE [dbo].[Products] DROP COLUMN [ManufacturerAddress];
IF COL_LENGTH(N'dbo.Products', N'ManufacturerName') IS NOT NULL
    ALTER TABLE [dbo].[Products] DROP COLUMN [ManufacturerName];
IF COL_LENGTH(N'dbo.Products', N'CountryOfOrigin') IS NOT NULL
    ALTER TABLE [dbo].[Products] DROP COLUMN [CountryOfOrigin];
IF COL_LENGTH(N'dbo.Products', N'Unit') IS NOT NULL
    ALTER TABLE [dbo].[Products] DROP COLUMN [Unit];
IF COL_LENGTH(N'dbo.Products', N'ModelNumber') IS NOT NULL
    ALTER TABLE [dbo].[Products] DROP COLUMN [ModelNumber];
""");
        }
    }
}
