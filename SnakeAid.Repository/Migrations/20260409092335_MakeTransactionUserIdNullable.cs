using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class MakeTransactionUserIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "SnakeAid",
                table: "Transactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "SnakeAid"."Transactions"
                        WHERE "UserId" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Cannot rollback MakeTransactionUserIdNullable while Transactions.UserId contains NULL rows. Reassign or delete platform-owned transactions before downgrading.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "SnakeAid",
                table: "Transactions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
