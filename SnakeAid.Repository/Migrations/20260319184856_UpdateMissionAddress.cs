using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMissionAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedCost",
                schema: "SnakeAid",
                table: "RescueMissions");

            migrationBuilder.RenameColumn(
                name: "HospitalTransferPrice",
                schema: "SnakeAid",
                table: "RescueMissions",
                newName: "CostFromCenter");

            migrationBuilder.RenameColumn(
                name: "DistanceToHospitalKm",
                schema: "SnakeAid",
                table: "RescueMissions",
                newName: "DistanceFromCenterKm");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.RenameColumn(
                name: "DistanceFromCenterKm",
                schema: "SnakeAid",
                table: "RescueMissions",
                newName: "DistanceToHospitalKm");

            migrationBuilder.RenameColumn(
                name: "CostFromCenter",
                schema: "SnakeAid",
                table: "RescueMissions",
                newName: "HospitalTransferPrice");

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCost",
                schema: "SnakeAid",
                table: "RescueMissions",
                type: "numeric(18,2)",
                nullable: true);
        }
    }
}
