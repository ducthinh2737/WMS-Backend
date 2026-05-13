using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Infrastructure.Migrations
{
    public partial class ApplyRenameToDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sử dụng khối SQL để bỏ qua lỗi nếu bảng đã tồn tại hoặc bảng cũ không tồn tại
            // Đổi tên bảng Sales -> Outbound
            migrationBuilder.Sql(@"
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'WmsDb222' AND TABLE_NAME = 'SalesOrders');
                SET @cmd = IF(@exist > 0, 'RENAME TABLE SalesOrders TO OutboundOrders', 'SELECT 1');
                PREPARE stmt FROM @cmd;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'WmsDb222' AND TABLE_NAME = 'SalesOrderItems');
                SET @cmd = IF(@exist > 0, 'RENAME TABLE SalesOrderItems TO OutboundOrderItems', 'SELECT 1');
                PREPARE stmt FROM @cmd;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            // Đổi tên các cột trong OutboundOrderItems
            migrationBuilder.Sql(@"
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'WmsDb222' AND TABLE_NAME = 'OutboundOrderItems' AND COLUMN_NAME = 'SalesOrderId');
                SET @cmd = IF(@exist > 0, 'ALTER TABLE OutboundOrderItems CHANGE COLUMN SalesOrderId OutboundOrderId CHAR(36)', 'SELECT 1');
                PREPARE stmt FROM @cmd;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            // Đổi tên các cột trong GoodsIssues
            migrationBuilder.Sql(@"
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'WmsDb222' AND TABLE_NAME = 'GoodsIssues' AND COLUMN_NAME = 'SalesOrderId');
                SET @cmd = IF(@exist > 0, 'ALTER TABLE GoodsIssues CHANGE COLUMN SalesOrderId OutboundOrderId CHAR(36)', 'SELECT 1');
                PREPARE stmt FROM @cmd;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            // Đổi tên các cột trong GoodsIssueItems
            migrationBuilder.Sql(@"
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'WmsDb222' AND TABLE_NAME = 'GoodsIssueItems' AND COLUMN_NAME = 'SalesOrderItemId');
                SET @cmd = IF(@exist > 0, 'ALTER TABLE GoodsIssueItems CHANGE COLUMN SalesOrderItemId OutboundOrderItemId CHAR(36)', 'SELECT 1');
                PREPARE stmt FROM @cmd;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
            
            // Xử lý nốt phần Inbound nếu cột chưa đổi tên
            migrationBuilder.Sql(@"
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'WmsDb222' AND TABLE_NAME = 'InboundOrderItems' AND COLUMN_NAME = 'PurchaseOrderId');
                SET @cmd = IF(@exist > 0, 'ALTER TABLE InboundOrderItems CHANGE COLUMN PurchaseOrderId InboundOrderId CHAR(36)', 'SELECT 1');
                PREPARE stmt FROM @cmd;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
