using Microsoft.EntityFrameworkCore;

namespace TMDT1_TH.Data.Database;

public static class DatabaseTriggerInstaller
{
    public static async Task TryInstallAsync(IServiceProvider services, ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            if (!await dbContext.Database.CanConnectAsync())
            {
                logger.LogWarning("Chưa kết nối được database. Hãy chạy Add-Migration và Update-Database.");
                return;
            }

            await dbContext.Database.ExecuteSqlRawAsync(ValidationTriggerSql);
            await dbContext.Database.ExecuteSqlRawAsync(HistoryTriggerSql);
            logger.LogInformation("Đã cài/cập nhật trigger quản lý lịch giá.");
        }
        catch (Exception exception)
        {
            // Không chặn website khởi động nếu người dùng chưa chạy migration.
            logger.LogWarning(exception,
                "Chưa thể cài trigger. Hãy bảo đảm đã chạy Update-Database rồi khởi động lại ứng dụng.");
        }
    }

    private const string ValidationTriggerSql = """
CREATE OR ALTER TRIGGER [dbo].[TRG_PriceSchedules_Validate]
ON [dbo].[PriceSchedules]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS i
        INNER JOIN [dbo].[PriceSchedules] AS p
            ON p.[Id] <> i.[Id]
           AND p.[MarketId] = i.[MarketId]
           AND p.[IsActive] = 1
           AND i.[IsActive] = 1
           AND
           (
               (i.[ProductId] IS NOT NULL AND p.[ProductId] = i.[ProductId])
               OR
               (i.[ProductVariantId] IS NOT NULL AND p.[ProductVariantId] = i.[ProductVariantId])
           )
           AND i.[ValidFrom] < COALESCE(p.[ValidTo], CONVERT(datetime2, '9999-12-31'))
           AND p.[ValidFrom] < COALESCE(i.[ValidTo], CONVERT(datetime2, '9999-12-31'))
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51001, N'Khoảng thời gian giá bị chồng lấn với một lịch giá đang hoạt động cùng thị trường.', 1;
    END
END;
""";

    private const string HistoryTriggerSql = """
CREATE OR ALTER TRIGGER [dbo].[TRG_PriceSchedules_History]
ON [dbo].[PriceSchedules]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[PriceHistories]
    (
        [PriceScheduleId],
        [ProductId],
        [ProductVariantId],
        [MarketId],
        [OldCostPrice],
        [NewCostPrice],
        [OldListPrice],
        [NewListPrice],
        [OldSalePrice],
        [NewSalePrice],
        [OldValidFrom],
        [NewValidFrom],
        [OldValidTo],
        [NewValidTo],
        [Action],
        [ChangedBy],
        [ChangedAt],
        [Reason],
        [CreatedAt],
        [CreatedBy]
    )
    SELECT
        COALESCE(i.[Id], d.[Id]),
        COALESCE(i.[ProductId], d.[ProductId]),
        COALESCE(i.[ProductVariantId], d.[ProductVariantId]),
        COALESCE(i.[MarketId], d.[MarketId]),
        d.[CostPrice],
        i.[CostPrice],
        d.[ListPrice],
        i.[ListPrice],
        d.[SalePrice],
        i.[SalePrice],
        d.[ValidFrom],
        i.[ValidFrom],
        d.[ValidTo],
        i.[ValidTo],
        CASE
            WHEN d.[Id] IS NULL THEN 1
            WHEN i.[Id] IS NULL THEN 3
            ELSE 2
        END,
        COALESCE(i.[UpdatedBy], i.[CreatedBy], d.[UpdatedBy], d.[CreatedBy], SUSER_SNAME(), N'System'),
        SYSUTCDATETIME(),
        COALESCE(i.[Note], d.[Note]),
        SYSUTCDATETIME(),
        N'TRIGGER'
    FROM inserted AS i
    FULL OUTER JOIN deleted AS d ON i.[Id] = d.[Id];
END;
""";
}
