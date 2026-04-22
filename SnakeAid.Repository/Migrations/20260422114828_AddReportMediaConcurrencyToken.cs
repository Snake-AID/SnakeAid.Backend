using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddReportMediaConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "SnakeAid",
                table: "ReportMedias",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "SnakeAid",
                table: "ReportMedias");
        }
    }
}
