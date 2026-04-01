using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBlog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                schema: "SnakeAid",
                table: "Blogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LikeCount",
                schema: "SnakeAid",
                table: "Blogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<string>>(
                name: "LikedViewer",
                schema: "SnakeAid",
                table: "Blogs",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "ReadingTime",
                schema: "SnakeAid",
                table: "Blogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int[]>(
                name: "Tags",
                schema: "SnakeAid",
                table: "Blogs",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                schema: "SnakeAid",
                table: "Blogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                schema: "SnakeAid",
                table: "Blogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                schema: "SnakeAid",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "LikeCount",
                schema: "SnakeAid",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "LikedViewer",
                schema: "SnakeAid",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "ReadingTime",
                schema: "SnakeAid",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "Tags",
                schema: "SnakeAid",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                schema: "SnakeAid",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                schema: "SnakeAid",
                table: "Blogs");
        }
    }
}
