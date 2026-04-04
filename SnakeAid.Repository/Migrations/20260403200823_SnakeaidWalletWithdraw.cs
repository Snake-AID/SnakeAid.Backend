using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class SnakeaidWalletWithdraw : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountHolderName",
                schema: "SnakeAid",
                table: "WalletWithdraws",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                schema: "SnakeAid",
                table: "WalletWithdraws",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankBin",
                schema: "SnakeAid",
                table: "WalletWithdraws",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessedByAdminId",
                schema: "SnakeAid",
                table: "WalletWithdraws",
                type: "uuid",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_WalletWithdraws_ProcessedByAdminId",
                schema: "SnakeAid",
                table: "WalletWithdraws",
                column: "ProcessedByAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_WalletWithdraws_Accounts_ProcessedByAdminId",
                schema: "SnakeAid",
                table: "WalletWithdraws",
                column: "ProcessedByAdminId",
                principalSchema: "AspNetIdentity",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WalletWithdraws_Accounts_ProcessedByAdminId",
                schema: "SnakeAid",
                table: "WalletWithdraws");

            migrationBuilder.DropIndex(
                name: "IX_WalletWithdraws_ProcessedByAdminId",
                schema: "SnakeAid",
                table: "WalletWithdraws");

            migrationBuilder.DropColumn(
                name: "AccountHolderName",
                schema: "SnakeAid",
                table: "WalletWithdraws");

            migrationBuilder.DropColumn(
                name: "AdminNotes",
                schema: "SnakeAid",
                table: "WalletWithdraws");

            migrationBuilder.DropColumn(
                name: "BankBin",
                schema: "SnakeAid",
                table: "WalletWithdraws");

            migrationBuilder.DropColumn(
                name: "ProcessedByAdminId",
                schema: "SnakeAid",
                table: "WalletWithdraws");

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
