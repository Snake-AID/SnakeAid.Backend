using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingDbSetsAndFixRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FirstAidGuidelines_VenomTypes_VenomTypeId",
                schema: "SnakeAidDb",
                table: "FirstAidGuidelines");

            migrationBuilder.DropIndex(
                name: "IX_VenomTypes_FirstAidGuidelineId",
                schema: "SnakeAidDb",
                table: "VenomTypes");

            migrationBuilder.DropIndex(
                name: "IX_FirstAidGuidelines_VenomTypeId",
                schema: "SnakeAidDb",
                table: "FirstAidGuidelines");

            migrationBuilder.DropColumn(
                name: "VenomTypeId",
                schema: "SnakeAidDb",
                table: "FirstAidGuidelines");

            migrationBuilder.CreateIndex(
                name: "IX_VenomTypes_FirstAidGuidelineId",
                schema: "SnakeAidDb",
                table: "VenomTypes",
                column: "FirstAidGuidelineId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VenomTypes_FirstAidGuidelineId",
                schema: "SnakeAidDb",
                table: "VenomTypes");

            migrationBuilder.AddColumn<int>(
                name: "VenomTypeId",
                schema: "SnakeAidDb",
                table: "FirstAidGuidelines",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VenomTypes_FirstAidGuidelineId",
                schema: "SnakeAidDb",
                table: "VenomTypes",
                column: "FirstAidGuidelineId");

            migrationBuilder.CreateIndex(
                name: "IX_FirstAidGuidelines_VenomTypeId",
                schema: "SnakeAidDb",
                table: "FirstAidGuidelines",
                column: "VenomTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_FirstAidGuidelines_VenomTypes_VenomTypeId",
                schema: "SnakeAidDb",
                table: "FirstAidGuidelines",
                column: "VenomTypeId",
                principalSchema: "SnakeAidDb",
                principalTable: "VenomTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
