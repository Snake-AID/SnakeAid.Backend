using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePostGis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                schema: "SnakeAid",
                table: "RescuerProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DistanceToHospitalKm",
                schema: "SnakeAid",
                table: "RescueMissions",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HospitalId",
                schema: "SnakeAid",
                table: "RescueMissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HospitalTransferPrice",
                schema: "SnakeAid",
                table: "RescueMissions",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresHospitalization",
                schema: "SnakeAid",
                table: "RescueMissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentFacilities_Location",
                schema: "SnakeAid",
                table: "TreatmentFacilities",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_RescuerProfiles_IsAvailable",
                schema: "SnakeAid",
                table: "RescuerProfiles",
                column: "IsAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_RescuerProfiles_LastLocation",
                schema: "SnakeAid",
                table: "RescuerProfiles",
                column: "LastLocation")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_RescueMissions_HospitalId",
                schema: "SnakeAid",
                table: "RescueMissions",
                column: "HospitalId");

            migrationBuilder.AddForeignKey(
                name: "FK_RescueMissions_TreatmentFacilities_HospitalId",
                schema: "SnakeAid",
                table: "RescueMissions",
                column: "HospitalId",
                principalSchema: "SnakeAid",
                principalTable: "TreatmentFacilities",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RescueMissions_TreatmentFacilities_HospitalId",
                schema: "SnakeAid",
                table: "RescueMissions");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentFacilities_Location",
                schema: "SnakeAid",
                table: "TreatmentFacilities");

            migrationBuilder.DropIndex(
                name: "IX_RescuerProfiles_IsAvailable",
                schema: "SnakeAid",
                table: "RescuerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_RescuerProfiles_LastLocation",
                schema: "SnakeAid",
                table: "RescuerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_RescueMissions_HospitalId",
                schema: "SnakeAid",
                table: "RescueMissions");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                schema: "SnakeAid",
                table: "RescuerProfiles");

            migrationBuilder.DropColumn(
                name: "DistanceToHospitalKm",
                schema: "SnakeAid",
                table: "RescueMissions");

            migrationBuilder.DropColumn(
                name: "HospitalId",
                schema: "SnakeAid",
                table: "RescueMissions");

            migrationBuilder.DropColumn(
                name: "HospitalTransferPrice",
                schema: "SnakeAid",
                table: "RescueMissions");

            migrationBuilder.DropColumn(
                name: "RequiresHospitalization",
                schema: "SnakeAid",
                table: "RescueMissions");
        }
    }
}
