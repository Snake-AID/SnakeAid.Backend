using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerReportToConsultation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerReport",
                schema: "SnakeAid",
                table: "Consultations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerReportSubmittedAt",
                schema: "SnakeAid",
                table: "Consultations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerReport",
                schema: "SnakeAid",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "CustomerReportSubmittedAt",
                schema: "SnakeAid",
                table: "Consultations");
        }
    }
}
