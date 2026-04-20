using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class ConvertConsultationBookingCancellationReasonToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "SnakeAid",
                table: "ConsultationBookings");

            migrationBuilder.AddColumn<int>(
                schema: "SnakeAid",
                table: "ConsultationBookings",
                name: "CancellationReason",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "SnakeAid",
                table: "ConsultationBookings");

            migrationBuilder.AddColumn<string>(
                schema: "SnakeAid",
                table: "ConsultationBookings",
                name: "CancellationReason",
                type: "text",
                nullable: true);
        }
    }
}
