using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchemaV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RescueMissions_IncidentId",
                schema: "SnakeAid",
                table: "RescueMissions");

            migrationBuilder.CreateIndex(
                name: "IX_RescueMissions_IncidentId",
                schema: "SnakeAid",
                table: "RescueMissions",
                column: "IncidentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RescueMissions_IncidentId",
                schema: "SnakeAid",
                table: "RescueMissions");

            migrationBuilder.CreateIndex(
                name: "IX_RescueMissions_IncidentId",
                schema: "SnakeAid",
                table: "RescueMissions",
                column: "IncidentId",
                unique: true);
        }
    }
}
