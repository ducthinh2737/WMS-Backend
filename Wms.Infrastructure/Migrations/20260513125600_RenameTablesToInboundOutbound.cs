using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesToInboundOutbound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Rename Tables
            migrationBuilder.RenameTable(name: "PurchaseOrders", newName: "InboundOrders");
            migrationBuilder.RenameTable(name: "PurchaseOrderItems", newName: "InboundOrderItems");
            migrationBuilder.RenameTable(name: "SalesOrders", newName: "OutboundOrders");
            migrationBuilder.RenameTable(name: "SalesOrderItems", newName: "OutboundOrderItems");

            // 2. Rename Columns in related tables
            // In InboundOrderItems
            migrationBuilder.RenameColumn(name: "PurchaseOrderId", table: "InboundOrderItems", newName: "InboundOrderId");
            
            // In OutboundOrderItems
            migrationBuilder.RenameColumn(name: "SalesOrderId", table: "OutboundOrderItems", newName: "OutboundOrderId");

            // In GoodsReceipts
            migrationBuilder.RenameColumn(name: "PurchaseOrderId", table: "GoodsReceipts", newName: "InboundOrderId");

            // In GoodsIssues
            migrationBuilder.RenameColumn(name: "SalesOrderId", table: "GoodsIssues", newName: "OutboundOrderId");

            // In GoodsIssueItems
            migrationBuilder.RenameColumn(name: "SalesOrderItemId", table: "GoodsIssueItems", newName: "OutboundOrderItemId");

            // 3. Rename Foreign Keys (Optional but good for consistency)
            // Note: EF Core usually handles renaming indexes, but we can be explicit if needed.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "InboundOrders", newName: "PurchaseOrders");
            migrationBuilder.RenameTable(name: "InboundOrderItems", newName: "PurchaseOrderItems");
            migrationBuilder.RenameTable(name: "OutboundOrders", newName: "SalesOrders");
            migrationBuilder.RenameTable(name: "OutboundOrderItems", newName: "SalesOrderItems");

            migrationBuilder.RenameColumn(name: "InboundOrderId", table: "PurchaseOrderItems", newName: "PurchaseOrderId");
            migrationBuilder.RenameColumn(name: "OutboundOrderId", table: "SalesOrderItems", newName: "SalesOrderId");
            migrationBuilder.RenameColumn(name: "InboundOrderId", table: "GoodsReceipts", newName: "PurchaseOrderId");
            migrationBuilder.RenameColumn(name: "OutboundOrderId", table: "GoodsIssues", newName: "SalesOrderId");
            migrationBuilder.RenameColumn(name: "OutboundOrderItemId", table: "GoodsIssueItems", newName: "SalesOrderItemId");
        }
    }
}
