using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerAndAddressToGoodsIssue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "GoodsIssues",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "GoodsIssues",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssues_CustomerId",
                table: "GoodsIssues",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsIssues_Customers_CustomerId",
                table: "GoodsIssues",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsIssues_Customers_CustomerId",
                table: "GoodsIssues");

            migrationBuilder.DropIndex(
                name: "IX_GoodsIssues_CustomerId",
                table: "GoodsIssues");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "GoodsIssues");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "GoodsIssues");
        }
    }
}
