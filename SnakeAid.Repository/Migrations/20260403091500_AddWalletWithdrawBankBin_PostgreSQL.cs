using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SnakeAid.Repository.Data;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(SnakeAidDbContext))]
    [Migration("20260403091500_AddWalletWithdrawBankBin_PostgreSQL")]
    public partial class AddWalletWithdrawBankBin_PostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankBin",
                schema: "SnakeAid",
                table: "WalletWithdraws",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankBin",
                schema: "SnakeAid",
                table: "WalletWithdraws");
        }
    }
}
