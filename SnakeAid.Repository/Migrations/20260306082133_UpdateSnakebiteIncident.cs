using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SnakeAid.Core.Domains;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSnakebiteIncident : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SnakeCatchingMissions_RequestId",
                schema: "SnakeAid",
                table: "SnakeCatchingMissions");

            migrationBuilder.AddColumn<Guid>(
                name: "AIRecognitionResultId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<FilterAnswerData>(
                name: "FilterAnswers",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdentificationMethod",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "IdentifiedAt",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdentifiedSnakeSpeciesId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SnakeCatchingMissions_RequestId",
                schema: "SnakeAid",
                table: "SnakeCatchingMissions",
                column: "SnakeCatchingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SnakebiteIncidents_AIRecognitionResultId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                column: "AIRecognitionResultId");

            migrationBuilder.CreateIndex(
                name: "IX_SnakebiteIncidents_IdentifiedSnakeSpeciesId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                column: "IdentifiedSnakeSpeciesId");

            migrationBuilder.AddForeignKey(
                name: "FK_SnakebiteIncidents_SnakeAIRecognitionResults_AIRecognitionR~",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                column: "AIRecognitionResultId",
                principalSchema: "SnakeAid",
                principalTable: "SnakeAIRecognitionResults",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SnakebiteIncidents_SnakeSpecies_IdentifiedSnakeSpeciesId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                column: "IdentifiedSnakeSpeciesId",
                principalSchema: "SnakeAid",
                principalTable: "SnakeSpecies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SnakebiteIncidents_SnakeAIRecognitionResults_AIRecognitionR~",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropForeignKey(
                name: "FK_SnakebiteIncidents_SnakeSpecies_IdentifiedSnakeSpeciesId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropIndex(
                name: "IX_SnakeCatchingMissions_RequestId",
                schema: "SnakeAid",
                table: "SnakeCatchingMissions");

            migrationBuilder.DropIndex(
                name: "IX_SnakebiteIncidents_AIRecognitionResultId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropIndex(
                name: "IX_SnakebiteIncidents_IdentifiedSnakeSpeciesId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropColumn(
                name: "AIRecognitionResultId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropColumn(
                name: "FilterAnswers",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropColumn(
                name: "IdentificationMethod",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropColumn(
                name: "IdentifiedAt",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropColumn(
                name: "IdentifiedSnakeSpeciesId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeCatchingMissions_RequestId",
                schema: "SnakeAid",
                table: "SnakeCatchingMissions",
                column: "SnakeCatchingRequestId",
                unique: true);
        }
    }
}
