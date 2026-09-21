using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class AddNegotiation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowNegotiation",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailableForRent",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnSale",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RentPrice",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RentalConditions",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RentalDuration",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RentalSecurity",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SaleDetails",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseType",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RentalConditions",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RentalDuration",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RentalSecurity",
                table: "OrderDetails",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Negotiations",
                columns: table => new
                {
                    NegotiationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ShopkeeperId = table.Column<int>(type: "int", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RequestedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CounterPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AgreedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CustomerMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShopkeeperMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Negotiations", x => x.NegotiationId);
                    table.ForeignKey(
                        name: "FK_Negotiations_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId");
                    table.ForeignKey(
                        name: "FK_Negotiations_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Negotiations_Users_ShopkeeperId",
                        column: x => x.ShopkeeperId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Negotiations_CustomerId",
                table: "Negotiations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Negotiations_ProductId",
                table: "Negotiations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Negotiations_ShopkeeperId",
                table: "Negotiations",
                column: "ShopkeeperId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Negotiations");

            migrationBuilder.DropColumn(
                name: "AllowNegotiation",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsAvailableForRent",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsOnSale",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RentPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RentalConditions",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RentalDuration",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RentalSecurity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SaleDetails",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PurchaseType",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "RentalConditions",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "RentalDuration",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "RentalSecurity",
                table: "OrderDetails");
        }
    }
}
