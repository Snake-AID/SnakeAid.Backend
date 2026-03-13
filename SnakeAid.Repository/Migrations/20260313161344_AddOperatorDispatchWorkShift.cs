using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorDispatchWorkShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RescuerRequests_RescueRequestSessions_SessionId",
                schema: "SnakeAid",
                table: "RescuerRequests");

            migrationBuilder.DropTable(
                name: "RescueRequestSessions",
                schema: "SnakeAid");

            migrationBuilder.DropIndex(
                name: "IX_RescuerRequests_ExpiredAt",
                schema: "SnakeAid",
                table: "RescuerRequests");

            migrationBuilder.DropIndex(
                name: "IX_RescuerRequests_SessionId",
                schema: "SnakeAid",
                table: "RescuerRequests");

            migrationBuilder.DropColumn(
                name: "CurrentRadiusKm",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropColumn(
                name: "CurrentSessionNumber",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropColumn(
                name: "ExpiredAt",
                schema: "SnakeAid",
                table: "RescuerRequests");

            migrationBuilder.DropColumn(
                name: "SessionId",
                schema: "SnakeAid",
                table: "RescuerRequests");

            migrationBuilder.RenameColumn(
                name: "LastSessionAt",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                newName: "DispatchedAt");

            migrationBuilder.RenameColumn(
                name: "RequestSentAt",
                schema: "SnakeAid",
                table: "RescuerRequests",
                newName: "DispatchedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DispatchedAt",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HandlingOperatorId",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatorNotes",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HandlingOperatorId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatorNotes",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "DeclineReason",
                schema: "SnakeAid",
                table: "RescuerRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OperatorId",
                schema: "SnakeAid",
                table: "RescuerRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IncidentCallLogs",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: true),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentCallLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentCallLogs_Accounts_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncidentCallLogs_SnakebiteIncidents_IncidentId",
                        column: x => x.IncidentId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakebiteIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperatorProfiles",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsOnDuty = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentCaseCount = table.Column<int>(type: "integer", nullable: false),
                    MaxConcurrentCases = table.Column<int>(type: "integer", nullable: false),
                    IsAcceptingNew = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatorProfiles_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkShifts",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredRescuers = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkShifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShiftAssignments",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RescuerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CheckInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckOutAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftAssignments_RescuerProfiles_RescuerId",
                        column: x => x.RescuerId,
                        principalSchema: "SnakeAid",
                        principalTable: "RescuerProfiles",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftAssignments_WorkShifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "SnakeAid",
                        principalTable: "WorkShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SnakeCatchingRequests_HandlingOperatorId",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests",
                column: "HandlingOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SnakebiteIncidents_HandlingOperatorId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                column: "HandlingOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_RescuerRequests_DispatchedAt",
                schema: "SnakeAid",
                table: "RescuerRequests",
                column: "DispatchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RescuerRequests_OperatorId",
                schema: "SnakeAid",
                table: "RescuerRequests",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentCallLogs_CalledAt",
                schema: "SnakeAid",
                table: "IncidentCallLogs",
                column: "CalledAt");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentCallLogs_IncidentId",
                schema: "SnakeAid",
                table: "IncidentCallLogs",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentCallLogs_OperatorId",
                schema: "SnakeAid",
                table: "IncidentCallLogs",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentCallLogs_Outcome",
                schema: "SnakeAid",
                table: "IncidentCallLogs",
                column: "Outcome");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorProfiles_IsAcceptingNew",
                schema: "SnakeAid",
                table: "OperatorProfiles",
                column: "IsAcceptingNew");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorProfiles_IsOnDuty",
                schema: "SnakeAid",
                table: "OperatorProfiles",
                column: "IsOnDuty");

            migrationBuilder.CreateIndex(
                name: "UX_OperatorProfiles_AccountId",
                schema: "SnakeAid",
                table: "OperatorProfiles",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_Date",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_RescuerId",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                column: "RescuerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_ShiftId",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_Status",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_ShiftAssignments_Rescuer_Shift_Date",
                schema: "SnakeAid",
                table: "ShiftAssignments",
                columns: new[] { "RescuerId", "ShiftId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkShifts_Name",
                schema: "SnakeAid",
                table: "WorkShifts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_WorkShifts_StartTime_EndTime",
                schema: "SnakeAid",
                table: "WorkShifts",
                columns: new[] { "StartTime", "EndTime" });

            migrationBuilder.AddForeignKey(
                name: "FK_RescuerRequests_Accounts_OperatorId",
                schema: "SnakeAid",
                table: "RescuerRequests",
                column: "OperatorId",
                principalSchema: "AspNetIdentity",
                principalTable: "Accounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SnakebiteIncidents_Accounts_HandlingOperatorId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                column: "HandlingOperatorId",
                principalSchema: "AspNetIdentity",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SnakeCatchingRequests_Accounts_HandlingOperatorId",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests",
                column: "HandlingOperatorId",
                principalSchema: "AspNetIdentity",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RescuerRequests_Accounts_OperatorId",
                schema: "SnakeAid",
                table: "RescuerRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SnakebiteIncidents_Accounts_HandlingOperatorId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropForeignKey(
                name: "FK_SnakeCatchingRequests_Accounts_HandlingOperatorId",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests");

            migrationBuilder.DropTable(
                name: "IncidentCallLogs",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "OperatorProfiles",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "ShiftAssignments",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "WorkShifts",
                schema: "SnakeAid");

            migrationBuilder.DropIndex(
                name: "IX_SnakeCatchingRequests_HandlingOperatorId",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests");

            migrationBuilder.DropIndex(
                name: "IX_SnakebiteIncidents_HandlingOperatorId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropIndex(
                name: "IX_RescuerRequests_DispatchedAt",
                schema: "SnakeAid",
                table: "RescuerRequests");

            migrationBuilder.DropIndex(
                name: "IX_RescuerRequests_OperatorId",
                schema: "SnakeAid",
                table: "RescuerRequests");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests");

            migrationBuilder.DropColumn(
                name: "DispatchedAt",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests");

            migrationBuilder.DropColumn(
                name: "HandlingOperatorId",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests");

            migrationBuilder.DropColumn(
                name: "OperatorNotes",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropColumn(
                name: "HandlingOperatorId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropColumn(
                name: "OperatorNotes",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "SnakeAid",
                table: "SnakebiteIncidents");

            migrationBuilder.DropColumn(
                name: "DeclineReason",
                schema: "SnakeAid",
                table: "RescuerRequests");

            migrationBuilder.DropColumn(
                name: "OperatorId",
                schema: "SnakeAid",
                table: "RescuerRequests");

            migrationBuilder.RenameColumn(
                name: "DispatchedAt",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                newName: "LastSessionAt");

            migrationBuilder.RenameColumn(
                name: "DispatchedAt",
                schema: "SnakeAid",
                table: "RescuerRequests",
                newName: "RequestSentAt");

            migrationBuilder.AddColumn<int>(
                name: "CurrentRadiusKm",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentSessionNumber",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredAt",
                schema: "SnakeAid",
                table: "RescuerRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                schema: "SnakeAid",
                table: "RescuerRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "RescueRequestSessions",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RadiusKm = table.Column<int>(type: "integer", nullable: false),
                    RescuersPinged = table.Column<int>(type: "integer", nullable: false),
                    SessionNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TriggerType = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RescueRequestSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RescueRequestSessions_SnakebiteIncidents_IncidentId",
                        column: x => x.IncidentId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakebiteIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RescuerRequests_ExpiredAt",
                schema: "SnakeAid",
                table: "RescuerRequests",
                column: "ExpiredAt");

            migrationBuilder.CreateIndex(
                name: "IX_RescuerRequests_SessionId",
                schema: "SnakeAid",
                table: "RescuerRequests",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RescueRequestSessions_IncidentId",
                schema: "SnakeAid",
                table: "RescueRequestSessions",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_RescueRequestSessions_IncidentId_SessionNumber",
                schema: "SnakeAid",
                table: "RescueRequestSessions",
                columns: new[] { "IncidentId", "SessionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RescueRequestSessions_Status",
                schema: "SnakeAid",
                table: "RescueRequestSessions",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_RescuerRequests_RescueRequestSessions_SessionId",
                schema: "SnakeAid",
                table: "RescuerRequests",
                column: "SessionId",
                principalSchema: "SnakeAid",
                principalTable: "RescueRequestSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
