using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCatchingEnvironmentForMission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CatchingEnvironmentId",
                schema: "SnakeAid",
                table: "SnakeCatchingMissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ReferenceId",
                schema: "SnakeAid",
                table: "ReportMedias",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeCatchingMissions_CatchingEnvironmentId",
                schema: "SnakeAid",
                table: "SnakeCatchingMissions",
                column: "CatchingEnvironmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_SnakeCatchingMissions_CatchingEnvironments_CatchingEnvironm~",
                schema: "SnakeAid",
                table: "SnakeCatchingMissions",
                column: "CatchingEnvironmentId",
                principalSchema: "SnakeAid",
                principalTable: "CatchingEnvironments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SnakeCatchingMissions_CatchingEnvironments_CatchingEnvironm~",
                schema: "SnakeAid",
                table: "SnakeCatchingMissions");

            migrationBuilder.DropIndex(
                name: "IX_SnakeCatchingMissions_CatchingEnvironmentId",
                schema: "SnakeAid",
                table: "SnakeCatchingMissions");

            migrationBuilder.DropColumn(
                name: "CatchingEnvironmentId",
                schema: "SnakeAid",
                table: "SnakeCatchingMissions");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReferenceId",
                schema: "SnakeAid",
                table: "ReportMedias",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
