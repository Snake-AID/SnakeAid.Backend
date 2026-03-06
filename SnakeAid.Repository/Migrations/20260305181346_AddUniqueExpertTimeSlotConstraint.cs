using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueExpertTimeSlotConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_ExpertTimeSlots_ExpertId_StartTime_EndTime",
                schema: "SnakeAid",
                table: "ExpertTimeSlots",
                columns: new[] { "ExpertId", "StartTime", "EndTime" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ExpertTimeSlots_ExpertId_StartTime_EndTime",
                schema: "SnakeAid",
                table: "ExpertTimeSlots");
        }
    }
}
