using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUomAndWmsStabilityFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Products",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Receipt_Qty",
                table: "ProductionReceiptItems",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "ProductionReceiptItems",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                table: "ProductionReceiptItems",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "ProductionReceiptItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "OutboundOrderItems",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "Issued_Qty",
                table: "OutboundOrderItems",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                table: "OutboundOrderItems",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "OutboundOrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                table: "InventoryTransactions",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "InventoryTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AfterQty",
                table: "InventoryHistories",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantityChange",
                table: "InventoryHistories",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BeforeQty",
                table: "InventoryHistories",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "InventoryHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "Received_qty",
                table: "InboundOrderItems",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "InboundOrderItems",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                table: "InboundOrderItems",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "InboundOrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "Received_Qty",
                table: "GoodsReceiptItems",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "GoodsReceiptItems",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                table: "GoodsReceiptItems",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "GoodsReceiptItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "GoodsIssueItems",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "Issued_Qty",
                table: "GoodsIssueItems",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                table: "GoodsIssueItems",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "GoodsIssueItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProductUoms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    Factor = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    IsBaseUnit = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUoms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductUoms_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductUoms_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProductId_LocationId_LotId",
                table: "Inventories",
                columns: new[] { "ProductId", "LocationId", "LotId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductUoms_ProductId",
                table: "ProductUoms",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUoms_ProductId_UnitId",
                table: "ProductUoms",
                columns: new[] { "ProductId", "UnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductUoms_UnitId",
                table: "ProductUoms",
                column: "UnitId");

            // DATA MIGRATION: Backfill ProductUoms
            migrationBuilder.Sql(@"
                INSERT INTO ProductUoms (ProductId, UnitId, Factor, IsBaseUnit, CreatedAt)
                SELECT p.Id, p.UnitId, 1.0, 1, CURRENT_TIMESTAMP(6)
                FROM Products p
                WHERE NOT EXISTS (SELECT 1 FROM ProductUoms pu WHERE pu.ProductId = p.Id);
            ");

            // DATA MIGRATION: Backfill BaseQuantity and UnitId for legacy transactions
            migrationBuilder.Sql(@"
                UPDATE OutboundOrderItems o JOIN Products p ON o.ProductId = p.Id SET o.UnitId = p.UnitId, o.BaseQuantity = o.Quantity WHERE o.UnitId = 0;
                UPDATE InboundOrderItems o JOIN Products p ON o.ProductId = p.Id SET o.UnitId = p.UnitId, o.BaseQuantity = o.Quantity WHERE o.UnitId = 0;
                UPDATE GoodsIssueItems o JOIN Products p ON o.ProductId = p.Id SET o.UnitId = p.UnitId, o.BaseQuantity = o.Quantity WHERE o.UnitId = 0;
                UPDATE GoodsReceiptItems o JOIN Products p ON o.ProductId = p.Id SET o.UnitId = p.UnitId, o.BaseQuantity = o.Quantity WHERE o.UnitId = 0;
                UPDATE ProductionReceiptItems o JOIN Products p ON o.ProductId = p.Id SET o.UnitId = p.UnitId, o.BaseQuantity = o.Quantity WHERE o.UnitId = 0;
                UPDATE InventoryTransactions t JOIN Products p ON t.ProductId = p.Id SET t.UnitId = p.UnitId, t.BaseQuantity = t.Quantity WHERE t.UnitId = 0;
                UPDATE InventoryHistories h JOIN Products p ON h.ProductId = p.Id SET h.UnitId = p.UnitId, h.BaseQuantityChange = h.QuantityChange WHERE h.UnitId = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductUoms");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_ProductId_LocationId_LotId",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "ProductionReceiptItems");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "ProductionReceiptItems");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "OutboundOrderItems");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "OutboundOrderItems");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "AfterQty",
                table: "InventoryHistories");

            migrationBuilder.DropColumn(
                name: "BaseQuantityChange",
                table: "InventoryHistories");

            migrationBuilder.DropColumn(
                name: "BeforeQty",
                table: "InventoryHistories");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "InventoryHistories");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "InboundOrderItems");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "InboundOrderItems");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "GoodsReceiptItems");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "GoodsReceiptItems");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "GoodsIssueItems");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "GoodsIssueItems");

            migrationBuilder.AlterColumn<int>(
                name: "Receipt_Qty",
                table: "ProductionReceiptItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "ProductionReceiptItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "OutboundOrderItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<int>(
                name: "Issued_Qty",
                table: "OutboundOrderItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<int>(
                name: "Received_qty",
                table: "InboundOrderItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "InboundOrderItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<int>(
                name: "Received_Qty",
                table: "GoodsReceiptItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "GoodsReceiptItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "GoodsIssueItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<int>(
                name: "Issued_Qty",
                table: "GoodsIssueItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");
        }
    }
}
