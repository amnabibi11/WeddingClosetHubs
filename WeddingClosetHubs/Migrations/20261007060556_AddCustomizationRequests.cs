using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomizationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomizationRequests",
                columns: table => new
                {
                    CustomizationRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ShopId = table.Column<int>(type: "int", nullable: false),
                    SelectedSize = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Measurements = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferenceImages = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShopkeeperPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ShopkeeperNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomizationRequests", x => x.CustomizationRequestId);
                    table.ForeignKey(
                        name: "FK_CustomizationRequests_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId");
                    table.ForeignKey(
                        name: "FK_CustomizationRequests_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "ShopId");
                    table.ForeignKey(
                        name: "FK_CustomizationRequests_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomizationRequests_CustomerId",
                table: "CustomizationRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomizationRequests_ProductId",
                table: "CustomizationRequests",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomizationRequests_ShopId",
                table: "CustomizationRequests",
                column: "ShopId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomizationRequests");
        }
    }
}
