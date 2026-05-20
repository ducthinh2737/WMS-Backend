using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToGoodsIssueItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RowVersion",
                table: "GoodsIssueItems",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GoodsIssueItems");
        }
    }
}
