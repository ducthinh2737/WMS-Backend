using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorInventoryV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Robust cleanup for partially applied migrations in MySQL (supports older versions)
            migrationBuilder.Sql(@"
                SET @exists = (SELECT 1 FROM information_schema.columns WHERE table_name = 'InventoryHistories' AND column_name = 'LotCode' AND table_schema = DATABASE());
                SET @query = IF(@exists, 'ALTER TABLE InventoryHistories DROP COLUMN LotCode', 'SELECT 1');
                PREPARE stmt FROM @query; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");
            migrationBuilder.Sql(@"
                SET @exists = (SELECT 1 FROM information_schema.columns WHERE table_name = 'InventoryHistories' AND column_name = 'LotId' AND table_schema = DATABASE());
                SET @query = IF(@exists, 'ALTER TABLE InventoryHistories DROP COLUMN LotId', 'SELECT 1');
                PREPARE stmt FROM @query; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");
            migrationBuilder.Sql(@"
                SET @exists = (SELECT 1 FROM information_schema.tables WHERE table_name = 'InventoryTransactions' AND table_schema = DATABASE());
                SET @query = IF(@exists, 'DROP TABLE InventoryTransactions', 'SELECT 1');
                PREPARE stmt FROM @query; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");
            migrationBuilder.Sql(@"
                SET @exists = (SELECT 1 FROM information_schema.columns WHERE table_name = 'Inventories' AND column_name = 'RowVersion' AND table_schema = DATABASE());
                SET @query = IF(@exists, 'ALTER TABLE Inventories DROP COLUMN RowVersion', 'SELECT 1');
                PREPARE stmt FROM @query; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.AddColumn<string>(
                name: "LotCode",
                table: "InventoryHistories",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "LotId",
                table: "InventoryHistories",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "RowVersion",
                table: "Inventories",
                type: "datetime(6)",
                rowVersion: true,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ReferenceCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WarehouseId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LocationId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    LotId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_CreatedAt",
                table: "InventoryTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_Idempotency",
                table: "InventoryTransactions",
                columns: new[] { "ReferenceCode", "ProductId", "LotId", "LocationId", "ActionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ProductId",
                table: "InventoryTransactions",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "LotCode",
                table: "InventoryHistories");

            migrationBuilder.DropColumn(
                name: "LotId",
                table: "InventoryHistories");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Inventories");
        }
    }
}
