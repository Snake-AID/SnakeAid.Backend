using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIncidentAndCommunicationReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalDetails",
                schema: "SnakeAid",
                table: "CommunityReports");

            migrationBuilder.AddColumn<long>(
                name: "PayOsOrderCode",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<Point>(
                name: "LocationCoordinates",
                schema: "SnakeAid",
                table: "CommunityReports",
                type: "geometry(Point, 4326)",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geometry");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "SnakeAid",
                table: "CommunityReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SnakeSpeciesId",
                schema: "SnakeAid",
                table: "CommunityReports",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReports_SnakeSpeciesId",
                schema: "SnakeAid",
                table: "CommunityReports",
                column: "SnakeSpeciesId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommunityReports_SnakeSpecies_SnakeSpeciesId",
                schema: "SnakeAid",
                table: "CommunityReports",
                column: "SnakeSpeciesId",
                principalSchema: "SnakeAid",
                principalTable: "SnakeSpecies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommunityReports_SnakeSpecies_SnakeSpeciesId",
                schema: "SnakeAid",
                table: "CommunityReports");

            migrationBuilder.DropIndex(
                name: "IX_CommunityReports_SnakeSpeciesId",
                schema: "SnakeAid",
                table: "CommunityReports");

            migrationBuilder.DropColumn(
                name: "PayOsOrderCode",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "SnakeAid",
                table: "CommunityReports");

            migrationBuilder.DropColumn(
                name: "SnakeSpeciesId",
                schema: "SnakeAid",
                table: "CommunityReports");

            migrationBuilder.AlterColumn<Point>(
                name: "LocationCoordinates",
                schema: "SnakeAid",
                table: "CommunityReports",
                type: "geometry",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geometry(Point, 4326)");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalDetails",
                schema: "SnakeAid",
                table: "CommunityReports",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
