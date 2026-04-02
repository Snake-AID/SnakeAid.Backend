using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAppNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeepLink",
                schema: "SnakeAid",
                table: "AppNotifications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificationType",
                schema: "SnakeAid",
                table: "AppNotifications",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PayloadJson",
                schema: "SnakeAid",
                table: "AppNotifications",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeepLink",
                schema: "SnakeAid",
                table: "AppNotifications");

            migrationBuilder.DropColumn(
                name: "NotificationType",
                schema: "SnakeAid",
                table: "AppNotifications");

            migrationBuilder.DropColumn(
                name: "PayloadJson",
                schema: "SnakeAid",
                table: "AppNotifications");
        }
    }
}
