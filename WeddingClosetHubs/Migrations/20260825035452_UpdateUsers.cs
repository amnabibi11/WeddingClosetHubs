using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryCompanyId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeliveryCompanies",
                columns: table => new
                {
                    DeliveryCompanyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryCompanies", x => x.DeliveryCompanyId);
                    table.ForeignKey(
                        name: "FK_DeliveryCompanies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DeliveryCompanyId",
                table: "Orders",
                column: "DeliveryCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryCompanies_UserId",
                table: "DeliveryCompanies",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_DeliveryCompanies_DeliveryCompanyId",
                table: "Orders",
                column: "DeliveryCompanyId",
                principalTable: "DeliveryCompanies",
                principalColumn: "DeliveryCompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_DeliveryCompanies_DeliveryCompanyId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "DeliveryCompanies");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DeliveryCompanyId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryCompanyId",
                table: "Orders");
        }
    }
}
