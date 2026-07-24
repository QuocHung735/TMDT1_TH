using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDT1_TH.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueBrandModelNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [dbo].[Products]
                SET [ModelNumber] = NULL
                WHERE [ModelNumber] IS NOT NULL
                  AND LTRIM(RTRIM([ModelNumber])) = N'';

                UPDATE [dbo].[Products]
                SET [ModelNumber] = LTRIM(RTRIM([ModelNumber]))
                WHERE [ModelNumber] IS NOT NULL
                  AND [ModelNumber] <> LTRIM(RTRIM([ModelNumber]));

                ;WITH [RankedModels] AS
                (
                    SELECT
                        [Id],
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY [BrandId], UPPER([ModelNumber])
                            ORDER BY [Id]
                        ) AS [DuplicateOrder]
                    FROM [dbo].[Products]
                    WHERE [IsDeleted] = 0
                      AND [ModelNumber] IS NOT NULL
                )
                UPDATE [product]
                SET [ModelNumber] = CONCAT(
                    N'LEGACY-P',
                    [product].[Id],
                    N'-',
                    LEFT(REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N''), 12))
                FROM [dbo].[Products] AS [product]
                INNER JOIN [RankedModels] AS [ranked]
                    ON [ranked].[Id] = [product].[Id]
                WHERE [ranked].[DuplicateOrder] > 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM [sys].[indexes]
                    WHERE [name] = N'UX_Products_BrandId_ModelNumber'
                      AND [object_id] = OBJECT_ID(N'[dbo].[Products]')
                )
                BEGIN
                    CREATE UNIQUE INDEX [UX_Products_BrandId_ModelNumber]
                    ON [dbo].[Products] ([BrandId], [ModelNumber])
                    WHERE [ModelNumber] IS NOT NULL AND [IsDeleted] = 0;
                END
                """);
}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS
                (
                    SELECT 1
                    FROM [sys].[indexes]
                    WHERE [name] = N'UX_Products_BrandId_ModelNumber'
                      AND [object_id] = OBJECT_ID(N'[dbo].[Products]')
                )
                BEGIN
                    DROP INDEX [UX_Products_BrandId_ModelNumber]
                    ON [dbo].[Products];
                END
                """);
}
    }
}
