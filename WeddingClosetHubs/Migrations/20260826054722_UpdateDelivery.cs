using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryCompanies");

            migrationBuilder.DropIndex(
                name: "IX_Shops_ShopkeeperId",
                table: "Shops");

            migrationBuilder.CreateTable(
                name: "DeliveryBoys",
                columns: table => new
                {
                    DeliveryBoyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CNIC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CNICFrontImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CNICBackImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MotorbikeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreferredZone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssignedZone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VerificationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AdminNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryBoys", x => x.DeliveryBoyId);
                    table.ForeignKey(
                        name: "FK_DeliveryBoys_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shops_ShopkeeperId",
                table: "Shops",
                column: "ShopkeeperId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryBoys_UserId",
                table: "DeliveryBoys",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryBoys");

            migrationBuilder.DropIndex(
                name: "IX_Shops_ShopkeeperId",
                table: "Shops");

            migrationBuilder.CreateTable(
                name: "DeliveryCompanies",
                columns: table => new
                {
                    DeliveryCompanyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryCompanies", x => x.DeliveryCompanyId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shops_ShopkeeperId",
                table: "Shops",
                column: "ShopkeeperId");
        }
    }
}
