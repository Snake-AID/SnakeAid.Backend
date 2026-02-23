using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class IgnoreMediaCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportMedias_SnakeCatchingRequests_SnakeCatchingRequestId",
                schema: "SnakeAid",
                table: "ReportMedias");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportMedias_SnakebiteIncidents_SnakebiteIncidentId",
                schema: "SnakeAid",
                table: "ReportMedias");

            migrationBuilder.DropIndex(
                name: "IX_ReportMedias_SnakebiteIncidentId",
                schema: "SnakeAid",
                table: "ReportMedias");

            migrationBuilder.DropIndex(
                name: "IX_ReportMedias_SnakeCatchingRequestId",
                schema: "SnakeAid",
                table: "ReportMedias");

            migrationBuilder.DropColumn(
                name: "SnakeCatchingRequestId",
                schema: "SnakeAid",
                table: "ReportMedias");

            migrationBuilder.DropColumn(
                name: "SnakebiteIncidentId",
                schema: "SnakeAid",
                table: "ReportMedias");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SnakeCatchingRequestId",
                schema: "SnakeAid",
                table: "ReportMedias",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SnakebiteIncidentId",
                schema: "SnakeAid",
                table: "ReportMedias",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportMedias_SnakebiteIncidentId",
                schema: "SnakeAid",
                table: "ReportMedias",
                column: "SnakebiteIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportMedias_SnakeCatchingRequestId",
                schema: "SnakeAid",
                table: "ReportMedias",
                column: "SnakeCatchingRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportMedias_SnakeCatchingRequests_SnakeCatchingRequestId",
                schema: "SnakeAid",
                table: "ReportMedias",
                column: "SnakeCatchingRequestId",
                principalSchema: "SnakeAid",
                principalTable: "SnakeCatchingRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportMedias_SnakebiteIncidents_SnakebiteIncidentId",
                schema: "SnakeAid",
                table: "ReportMedias",
                column: "SnakebiteIncidentId",
                principalSchema: "SnakeAid",
                principalTable: "SnakebiteIncidents",
                principalColumn: "Id");
        }
    }
}
