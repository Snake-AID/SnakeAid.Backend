using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShiftAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShiftAssignments_Date",
                schema: "SnakeAid",
                table: "ShiftAssignments");

            migrationBuilder.DropIndex(
                name: "UX_ShiftAssignments_Rescuer_Shift_Date",
                schema: "SnakeAid",
                table: "ShiftAssignments");

            migrationBuilder.DropColumn(
                name: "Date",
                schema: "SnakeAid",
                table: "ShiftAssignments");

            migrationBuilder.RenameColumn(
                name: "CheckOutAt",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                newName: "CheckOutAtUtc");

            migrationBuilder.RenameColumn(
                name: "CheckInAt",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                newName: "CheckInAtUtc");

            migrationBuilder.AddColumn<DateTime>(
                name: "ShiftEndLocal",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ShiftStartLocal",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_SnakebiteIncidents_Location",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                column: "LocationCoordinates")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_ShiftEndLocal",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                column: "ShiftEndLocal");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_ShiftStartLocal",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                column: "ShiftStartLocal");

            migrationBuilder.CreateIndex(
                name: "UX_ShiftAssignments_Rescuer_Shift_ShiftStartLocal",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                columns: new[] { "RescuerId", "ShiftId", "ShiftStartLocal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReports_Location",
                schema: "SnakeAid",
                table: "CommunityReports",
                column: "LocationCoordinates")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SnakebiteIncidents_Location",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropIndex(
                name: "IX_ShiftAssignments_ShiftEndLocal",
                schema: "SnakeAid",
                table: "ShiftAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ShiftAssignments_ShiftStartLocal",
                schema: "SnakeAid",
                table: "ShiftAssignments");

            migrationBuilder.DropIndex(
                name: "UX_ShiftAssignments_Rescuer_Shift_ShiftStartLocal",
                schema: "SnakeAid",
                table: "ShiftAssignments");

            migrationBuilder.DropIndex(
                name: "IX_CommunityReports_Location",
                schema: "SnakeAid",
                table: "CommunityReports");

            migrationBuilder.DropColumn(
                name: "ShiftEndLocal",
                schema: "SnakeAid",
                table: "ShiftAssignments");

            migrationBuilder.DropColumn(
                name: "ShiftStartLocal",
                schema: "SnakeAid",
                table: "ShiftAssignments");

            migrationBuilder.RenameColumn(
                name: "CheckOutAtUtc",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                newName: "CheckOutAt");

            migrationBuilder.RenameColumn(
                name: "CheckInAtUtc",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                newName: "CheckInAt");

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_Date",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "UX_ShiftAssignments_Rescuer_Shift_Date",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                columns: new[] { "RescuerId", "ShiftId", "Date" },
                unique: true);
        }
    }
}
