using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class ChangeProfileSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "SnakeAid",
                table: "RescuerRequests");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                schema: "SnakeAid",
                table: "RescuerProfiles");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                schema: "SnakeAid",
                table: "ExpertProfiles");

            migrationBuilder.DropColumn(
                name: "UnavailableReason",
                schema: "SnakeAid",
                table: "ExpertProfiles");

            migrationBuilder.CreateTable(
                name: "Otp",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OtpCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttemptLeft = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Otp", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Otp_Accounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Otp_UserId",
                schema: "SnakeAid",
                table: "Otp",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Otp",
                schema: "SnakeAid");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "SnakeAid",
                table: "RescuerRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                schema: "SnakeAid",
                table: "RescuerProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                schema: "SnakeAid",
                table: "ExpertProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UnavailableReason",
                schema: "SnakeAid",
                table: "ExpertProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
