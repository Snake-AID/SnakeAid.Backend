using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSnakeCatchingRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrePaid",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrePaidAt",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrePaid",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests");

            migrationBuilder.DropColumn(
                name: "PrePaidAt",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests");
        }
    }
}
