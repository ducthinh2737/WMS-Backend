using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManualVersionConcurrencyTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GoodsIssueItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GoodsIssueAllocates");

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "OutboundOrderItems",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Inventories",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "GoodsIssues",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "GoodsIssueItems",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "GoodsIssueAllocates",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.Sql("UPDATE OutboundOrderItems SET Version = 1;");
            migrationBuilder.Sql("UPDATE Inventories SET Version = 1;");
            migrationBuilder.Sql("UPDATE GoodsIssues SET Version = 1;");
            migrationBuilder.Sql("UPDATE GoodsIssueItems SET Version = 1;");
            migrationBuilder.Sql("UPDATE GoodsIssueAllocates SET Version = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "OutboundOrderItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "GoodsIssues");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "GoodsIssueItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "GoodsIssueAllocates");

            migrationBuilder.AddColumn<DateTime>(
                name: "RowVersion",
                table: "Inventories",
                type: "datetime(6)",
                rowVersion: true,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "RowVersion",
                table: "GoodsIssueItems",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "RowVersion",
                table: "GoodsIssueAllocates",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
