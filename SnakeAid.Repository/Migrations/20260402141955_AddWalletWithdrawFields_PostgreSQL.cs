using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletWithdrawFields_PostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VietQrImageBase64",
                schema: "SnakeAid",
                table: "WalletWithdraws",
                type: "text",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VietQrPayload",
                schema: "SnakeAid",
                table: "WalletWithdraws",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VietQrImageBase64",
                schema: "SnakeAid",
                table: "WalletWithdraws");

            migrationBuilder.DropColumn(
                name: "VietQrPayload",
                schema: "SnakeAid",
                table: "WalletWithdraws");
        }
    }
}
