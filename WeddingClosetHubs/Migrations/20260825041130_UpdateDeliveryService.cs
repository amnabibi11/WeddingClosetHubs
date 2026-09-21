using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeliveryService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryCompanies_Users_UserId",
                table: "DeliveryCompanies");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_DeliveryCompanies_DeliveryCompanyId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DeliveryCompanyId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryCompanies_UserId",
                table: "DeliveryCompanies");

            migrationBuilder.DropColumn(
                name: "DeliveryCompanyId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "DeliveryCompanies");

            migrationBuilder.RenameColumn(
                name: "Website",
                table: "DeliveryCompanies",
                newName: "RegistrationNumber");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "DeliveryCompanies",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "DeliveryCompanies",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "DeliveryCompanies");

            migrationBuilder.RenameColumn(
                name: "RegistrationNumber",
                table: "DeliveryCompanies",
                newName: "Website");

            migrationBuilder.AddColumn<int>(
                name: "DeliveryCompanyId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "DeliveryCompanies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "DeliveryCompanies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DeliveryCompanyId",
                table: "Orders",
                column: "DeliveryCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryCompanies_UserId",
                table: "DeliveryCompanies",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryCompanies_Users_UserId",
                table: "DeliveryCompanies",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_DeliveryCompanies_DeliveryCompanyId",
                table: "Orders",
                column: "DeliveryCompanyId",
                principalTable: "DeliveryCompanies",
                principalColumn: "DeliveryCompanyId");
        }
    }
}
