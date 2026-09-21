using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentAccount",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Users",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentAccount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Users");
        }
    }
}
