/*
  Chạy script này trong SSMS trên đúng database được cấu hình tại
  ConnectionStrings:DefaultConnection.

  Các lệnh GO là bắt buộc: chúng buộc SQL Server biên dịch lại schema
  sau khi các cột và bảng mới được tạo.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

IF OBJECT_ID(N'[dbo].[Products]', N'U') IS NULL
    THROW 51020, N'Không tìm thấy bảng Products. Hãy chạy migration khởi tạo database trước.', 1;

IF OBJECT_ID(N'[dbo].[ProductVariants]', N'U') IS NULL
    THROW 51021, N'Không tìm thấy bảng ProductVariants. Hãy chạy migration khởi tạo database trước.', 1;
GO

IF COL_LENGTH(N'dbo.Products', N'ModelNumber') IS NULL
    ALTER TABLE [dbo].[Products] ADD [ModelNumber] nvarchar(100) NULL;

IF COL_LENGTH(N'dbo.Products', N'Unit') IS NULL
    ALTER TABLE [dbo].[Products]
        ADD [Unit] nvarchar(50) NOT NULL
            CONSTRAINT [DF_Products_Unit] DEFAULT (N'Cái') WITH VALUES;

IF COL_LENGTH(N'dbo.Products', N'CountryOfOrigin') IS NULL
    ALTER TABLE [dbo].[Products] ADD [CountryOfOrigin] nvarchar(100) NULL;

IF COL_LENGTH(N'dbo.Products', N'ManufacturerName') IS NULL
    ALTER TABLE [dbo].[Products] ADD [ManufacturerName] nvarchar(250) NULL;

IF COL_LENGTH(N'dbo.Products', N'ManufacturerAddress') IS NULL
    ALTER TABLE [dbo].[Products] ADD [ManufacturerAddress] nvarchar(500) NULL;

IF COL_LENGTH(N'dbo.Products', N'WarrantyMonths') IS NULL
    ALTER TABLE [dbo].[Products] ADD [WarrantyMonths] int NULL;

IF COL_LENGTH(N'dbo.Products', N'LowStockThreshold') IS NULL
    ALTER TABLE [dbo].[Products]
        ADD [LowStockThreshold] int NOT NULL
            CONSTRAINT [DF_Products_LowStockThreshold] DEFAULT (5) WITH VALUES;

IF COL_LENGTH(N'dbo.Products', N'MinPurchaseQuantity') IS NULL
    ALTER TABLE [dbo].[Products]
        ADD [MinPurchaseQuantity] int NOT NULL
            CONSTRAINT [DF_Products_MinPurchaseQuantity] DEFAULT (1) WITH VALUES;

IF COL_LENGTH(N'dbo.Products', N'MaxPurchaseQuantity') IS NULL
    ALTER TABLE [dbo].[Products] ADD [MaxPurchaseQuantity] int NULL;

IF COL_LENGTH(N'dbo.Products', N'PackageLengthCm') IS NULL
    ALTER TABLE [dbo].[Products] ADD [PackageLengthCm] decimal(10, 2) NULL;

IF COL_LENGTH(N'dbo.Products', N'PackageWidthCm') IS NULL
    ALTER TABLE [dbo].[Products] ADD [PackageWidthCm] decimal(10, 2) NULL;

IF COL_LENGTH(N'dbo.Products', N'PackageHeightCm') IS NULL
    ALTER TABLE [dbo].[Products] ADD [PackageHeightCm] decimal(10, 2) NULL;

IF COL_LENGTH(N'dbo.ProductVariants', N'LowStockThreshold') IS NULL
    ALTER TABLE [dbo].[ProductVariants]
        ADD [LowStockThreshold] int NOT NULL
            CONSTRAINT [DF_ProductVariants_LowStockThreshold] DEFAULT (5) WITH VALUES;

IF COL_LENGTH(N'dbo.ProductVariants', N'SortOrder') IS NULL
    ALTER TABLE [dbo].[ProductVariants]
        ADD [SortOrder] int NOT NULL
            CONSTRAINT [DF_ProductVariants_SortOrder] DEFAULT (0) WITH VALUES;
GO

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
GO

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
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_ProductSpecifications_ProductId_DisplayOrder'
      AND [object_id] = OBJECT_ID(N'[dbo].[ProductSpecifications]')
)
    CREATE INDEX [IX_ProductSpecifications_ProductId_DisplayOrder]
        ON [dbo].[ProductSpecifications]([ProductId], [DisplayOrder]);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Products_LowStockThreshold')
    ALTER TABLE [dbo].[Products] WITH CHECK
        ADD CONSTRAINT [CK_Products_LowStockThreshold] CHECK ([LowStockThreshold] >= 0);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Products_PurchaseQuantity')
    ALTER TABLE [dbo].[Products] WITH CHECK
        ADD CONSTRAINT [CK_Products_PurchaseQuantity]
        CHECK ([MinPurchaseQuantity] >= 1
           AND ([MaxPurchaseQuantity] IS NULL OR [MaxPurchaseQuantity] >= [MinPurchaseQuantity]));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Products_PackageDimensions')
    ALTER TABLE [dbo].[Products] WITH CHECK
        ADD CONSTRAINT [CK_Products_PackageDimensions]
        CHECK (([PackageLengthCm] IS NULL OR [PackageLengthCm] >= 0)
           AND ([PackageWidthCm] IS NULL OR [PackageWidthCm] >= 0)
           AND ([PackageHeightCm] IS NULL OR [PackageHeightCm] >= 0));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Products_WarrantyMonths')
    ALTER TABLE [dbo].[Products] WITH CHECK
        ADD CONSTRAINT [CK_Products_WarrantyMonths]
        CHECK ([WarrantyMonths] IS NULL OR [WarrantyMonths] >= 0);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_ProductVariants_LowStockThreshold')
    ALTER TABLE [dbo].[ProductVariants] WITH CHECK
        ADD CONSTRAINT [CK_ProductVariants_LowStockThreshold] CHECK ([LowStockThreshold] >= 0);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_ProductVariants_SortOrder')
    ALTER TABLE [dbo].[ProductVariants] WITH CHECK
        ADD CONSTRAINT [CK_ProductVariants_SortOrder] CHECK ([SortOrder] >= 0);
GO

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
BEGIN
    ROLLBACK TRANSACTION;
    THROW 51022, N'Schema module sản phẩm chưa được nâng cấp đầy đủ.', 1;
END;

COMMIT TRANSACTION;
GO
