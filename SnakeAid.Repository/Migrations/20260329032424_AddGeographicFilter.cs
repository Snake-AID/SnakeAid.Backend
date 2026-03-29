using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddGeographicFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeographicRegions",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Boundary = table.Column<Polygon>(type: "geography(Polygon, 4326)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeographicRegions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegionSnakeMappings",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GeographicRegionId = table.Column<int>(type: "integer", nullable: false),
                    SnakeSpeciesId = table.Column<int>(type: "integer", nullable: false),
                    CommonLevel = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    DistributionNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionSnakeMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegionSnakeMappings_GeographicRegions_GeographicRegionId",
                        column: x => x.GeographicRegionId,
                        principalSchema: "SnakeAid",
                        principalTable: "GeographicRegions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegionSnakeMappings_SnakeSpecies_SnakeSpeciesId",
                        column: x => x.SnakeSpeciesId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeographicRegions_Boundary",
                schema: "SnakeAid",
                table: "GeographicRegions",
                column: "Boundary")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_GeographicRegions_Code",
                schema: "SnakeAid",
                table: "GeographicRegions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeographicRegions_DisplayOrder",
                schema: "SnakeAid",
                table: "GeographicRegions",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_GeographicRegions_IsActive",
                schema: "SnakeAid",
                table: "GeographicRegions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RegionSnakeMappings_CommonLevel",
                schema: "SnakeAid",
                table: "RegionSnakeMappings",
                column: "CommonLevel");

            migrationBuilder.CreateIndex(
                name: "IX_RegionSnakeMappings_GeographicRegionId_SnakeSpeciesId",
                schema: "SnakeAid",
                table: "RegionSnakeMappings",
                columns: new[] { "GeographicRegionId", "SnakeSpeciesId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegionSnakeMappings_IsActive",
                schema: "SnakeAid",
                table: "RegionSnakeMappings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RegionSnakeMappings_Priority",
                schema: "SnakeAid",
                table: "RegionSnakeMappings",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_RegionSnakeMappings_SnakeSpeciesId",
                schema: "SnakeAid",
                table: "RegionSnakeMappings",
                column: "SnakeSpeciesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegionSnakeMappings",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "GeographicRegions",
                schema: "SnakeAid");
        }
    }
}
