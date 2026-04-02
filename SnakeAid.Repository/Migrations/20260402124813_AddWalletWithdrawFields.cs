using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletWithdrawFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns for QR code support
            migrationBuilder.AddColumn<string>(
                name: "VietQrPayload",
                schema: "SnakeAid",
                table: "WalletWithdraws",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VietQrImageBase64",
                schema: "SnakeAid",
                table: "WalletWithdraws",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VietQrPayload",
                schema: "SnakeAid",
                table: "WalletWithdraws");

            migrationBuilder.DropColumn(
                name: "VietQrImageBase64",
                schema: "SnakeAid",
                table: "WalletWithdraws");
        }
    }
}
