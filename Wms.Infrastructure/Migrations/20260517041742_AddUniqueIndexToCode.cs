using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                SET @tablename = 'GoodsIssues';
                SET @indexname = 'IX_GoodsIssues_Code';
                SET @preparedStatement = (SELECT IF(
                  (
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE
                      TABLE_SCHEMA = @dbname
                      AND TABLE_NAME = @tablename
                      AND INDEX_NAME = @indexname
                  ) > 0,
                  'ALTER TABLE GoodsIssues DROP INDEX IX_GoodsIssues_Code;',
                  'SELECT 1;'
                ));
                PREPARE alterIfNotExists FROM @preparedStatement;
                EXECUTE alterIfNotExists;
                DEALLOCATE PREPARE alterIfNotExists;
                
                CREATE UNIQUE INDEX IX_GoodsIssues_Code ON GoodsIssues(Code);
            ");

            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                SET @tablename = 'OutboundOrders';
                SET @indexname = 'IX_OutboundOrders_Code';
                SET @preparedStatement = (SELECT IF(
                  (
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE
                      TABLE_SCHEMA = @dbname
                      AND TABLE_NAME = @tablename
                      AND INDEX_NAME = @indexname
                  ) > 0,
                  'ALTER TABLE OutboundOrders DROP INDEX IX_OutboundOrders_Code;',
                  'SELECT 1;'
                ));
                PREPARE alterIfNotExists FROM @preparedStatement;
                EXECUTE alterIfNotExists;
                DEALLOCATE PREPARE alterIfNotExists;
                
                CREATE UNIQUE INDEX IX_OutboundOrders_Code ON OutboundOrders(Code);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
