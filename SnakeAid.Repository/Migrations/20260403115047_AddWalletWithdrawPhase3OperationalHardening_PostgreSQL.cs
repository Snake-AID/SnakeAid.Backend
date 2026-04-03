using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletWithdrawPhase3OperationalHardening_PostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WalletWithdrawAudits",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WithdrawalId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: true),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DetailsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletWithdrawAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletWithdrawAudits_WalletWithdraws_WithdrawalId",
                        column: x => x.WithdrawalId,
                        principalSchema: "SnakeAid",
                        principalTable: "WalletWithdraws",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletWithdrawAudits_Action",
                schema: "SnakeAid",
                table: "WalletWithdrawAudits",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_WalletWithdrawAudits_WithdrawalId",
                schema: "SnakeAid",
                table: "WalletWithdrawAudits",
                column: "WithdrawalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WalletWithdrawAudits",
                schema: "SnakeAid");
        }
    }
}
