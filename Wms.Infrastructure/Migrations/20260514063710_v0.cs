using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename POIid to InboundOrderItemId in GoodsReceiptItems
            migrationBuilder.Sql(@"
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'WmsDb222' AND TABLE_NAME = 'GoodsReceiptItems' AND COLUMN_NAME = 'POIid');
                SET @cmd = IF(@exist > 0, 'ALTER TABLE GoodsReceiptItems CHANGE COLUMN POIid InboundOrderItemId CHAR(36)', 'SELECT 1');
                PREPARE stmt FROM @cmd;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'WmsDb222' AND TABLE_NAME = 'GoodsReceiptItems' AND COLUMN_NAME = 'InboundOrderItemId');
                SET @cmd = IF(@exist > 0, 'ALTER TABLE GoodsReceiptItems CHANGE COLUMN InboundOrderItemId POIid CHAR(36)', 'SELECT 1');
                PREPARE stmt FROM @cmd;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        }
    }
}
