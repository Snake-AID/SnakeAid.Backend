using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class SnakeSpeciesPrimaryVenom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ScientificName",
                schema: "SnakeAid",
                table: "VenomTypes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryVenomTypeId",
                schema: "SnakeAid",
                table: "SnakeSpecies",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SnakeSpecies_PrimaryVenomTypeId",
                schema: "SnakeAid",
                table: "SnakeSpecies",
                column: "PrimaryVenomTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_SnakeSpecies_PrimaryVenomType",
                schema: "SnakeAid",
                table: "SnakeSpecies",
                column: "PrimaryVenomTypeId",
                principalSchema: "SnakeAid",
                principalTable: "VenomTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SnakeSpecies_PrimaryVenomType",
                schema: "SnakeAid",
                table: "SnakeSpecies");

            migrationBuilder.DropIndex(
                name: "IX_SnakeSpecies_PrimaryVenomTypeId",
                schema: "SnakeAid",
                table: "SnakeSpecies");

            migrationBuilder.DropColumn(
                name: "PrimaryVenomTypeId",
                schema: "SnakeAid",
                table: "SnakeSpecies");

            migrationBuilder.AlterColumn<string>(
                name: "ScientificName",
                schema: "SnakeAid",
                table: "VenomTypes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }
    }
}
