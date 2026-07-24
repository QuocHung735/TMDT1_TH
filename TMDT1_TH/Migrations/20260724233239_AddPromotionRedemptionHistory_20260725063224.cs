using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDT1_TH.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionRedemptionHistory_20260725063224 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PromotionRedemptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromotionId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    CustomerUserId = table.Column<int>(type: "int", nullable: true),
                    PromotionCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    PromotionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsReleased = table.Column<bool>(type: "bit", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReleaseReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionRedemptions", x => x.Id);
                    table.CheckConstraint("CK_PromotionRedemptions_Discount", "[DiscountAmount] > 0");
                    table.CheckConstraint("CK_PromotionRedemptions_Release", "([IsReleased] = 0 AND [ReleasedAt] IS NULL) OR ([IsReleased] = 1 AND [ReleasedAt] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PromotionRedemptions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromotionRedemptions_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_CustomerUserId",
                table: "PromotionRedemptions",
                column: "CustomerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_OrderId",
                table: "PromotionRedemptions",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_PromotionId_IsReleased_RedeemedAt",
                table: "PromotionRedemptions",
                columns: new[] { "PromotionId", "IsReleased", "RedeemedAt" });
            migrationBuilder.Sql(
                """
                INSERT INTO [PromotionRedemptions]
                (
                    [PromotionId],
                    [OrderId],
                    [CustomerUserId],
                    [PromotionCode],
                    [PromotionName],
                    [DiscountAmount],
                    [RedeemedAt],
                    [IsReleased],
                    [ReleasedAt],
                    [ReleaseReason],
                    [CreatedAt],
                    [CreatedBy],
                    [UpdatedAt],
                    [UpdatedBy]
                )
                SELECT
                    p.[Id],
                    o.[Id],
                    o.[CustomerUserId],
                    o.[PromotionCode],
                    COALESCE(o.[PromotionName], p.[Name]),
                    o.[DiscountAmount],
                    o.[CreatedAt],
                    CASE WHEN o.[Status] = 6 THEN CAST(1 AS bit)
                         ELSE CAST(0 AS bit) END,
                    CASE WHEN o.[Status] = 6
                         THEN COALESCE(
                             o.[CancelledAt],
                             o.[UpdatedAt],
                             o.[CreatedAt])
                         ELSE NULL END,
                    CASE WHEN o.[Status] = 6
                         THEN COALESCE(
                             o.[CancellationReason],
                             N'Đơn đã hủy trước khi bổ sung lịch sử khuyến mãi.')
                         ELSE NULL END,
                    o.[CreatedAt],
                    N'Migration',
                    CASE WHEN o.[Status] = 6
                         THEN COALESCE(
                             o.[CancelledAt],
                             o.[UpdatedAt],
                             o.[CreatedAt])
                         ELSE NULL END,
                    CASE WHEN o.[Status] = 6
                         THEN N'Migration'
                         ELSE NULL END
                FROM [Orders] AS o
                INNER JOIN [Promotions] AS p
                    ON p.[Code] = o.[PromotionCode]
                WHERE
                    o.[PromotionCode] IS NOT NULL
                    AND o.[DiscountAmount] > 0
                    AND NOT EXISTS
                    (
                        SELECT 1
                        FROM [PromotionRedemptions] AS r
                        WHERE r.[OrderId] = o.[Id]
                    );

                UPDATE p
                SET p.[UsedCount] =
                    COALESCE(r.[ActiveCount], 0)
                FROM [Promotions] AS p
                LEFT JOIN
                (
                    SELECT
                        [PromotionId],
                        COUNT(*) AS [ActiveCount]
                    FROM [PromotionRedemptions]
                    WHERE [IsReleased] = 0
                    GROUP BY [PromotionId]
                ) AS r
                    ON r.[PromotionId] = p.[Id];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromotionRedemptions");
        }
    }
}

