using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDT1_TH.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceStoresStep8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Stores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AddressLine = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Ward = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    District = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReliabilityScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stores", x => x.Id);
                    table.CheckConstraint("CK_Stores_DisplayOrder", "[DisplayOrder] >= 0");
                    table.CheckConstraint("CK_Stores_ReliabilityScore", "[ReliabilityScore] IS NULL OR ([ReliabilityScore] >= 0 AND [ReliabilityScore] <= 100)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_Status",
                table: "Products",
                columns: new[] { "StoreId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Stores_IsActive_DisplayOrder_Name",
                table: "Stores",
                columns: new[] { "IsActive", "DisplayOrder", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Stores_Slug",
                table: "Stores",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.Sql(
                """
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM [Stores]
                    WHERE [Id] = 1
                )
                BEGIN
                    SET IDENTITY_INSERT [Stores] ON;

                    INSERT INTO [Stores]
                    (
                        [Id],
                        [Name],
                        [Slug],
                        [Description],
                        [LogoUrl],
                        [ContactEmail],
                        [PhoneNumber],
                        [AddressLine],
                        [Ward],
                        [District],
                        [Province],
                        [IsActive],
                        [IsVerified],
                        [ReliabilityScore],
                        [DisplayOrder],
                        [IsDeleted],
                        [CreatedAt],
                        [UpdatedAt],
                        [CreatedBy],
                        [UpdatedBy]
                    )
                    VALUES
                    (
                        1,
                        N'Mây Home Official',
                        N'may-home-official',
                        N'Cửa hàng chính thức mặc định của Mây Home.',
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        1,
                        1,
                        NULL,
                        0,
                        0,
                        '2026-01-01T00:00:00.0000000',
                        NULL,
                        N'System',
                        NULL
                    );

                    SET IDENTITY_INSERT [Stores] OFF;
                END;

                UPDATE [Products]
                SET [StoreId] = 1
                WHERE [StoreId] = 0;
                """);
            migrationBuilder.AddForeignKey(
                name: "FK_Products_Stores_StoreId",
                table: "Products",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Stores_StoreId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId_Status",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Products");
        }
    }
}
