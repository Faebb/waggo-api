using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Waggo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "email_change_attempts",
                schema: "identity",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "email_change_code_expires_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email_change_code_hash",
                schema: "identity",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_email",
                schema: "identity",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email_change_attempts",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_change_code_expires_at",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_change_code_hash",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "pending_email",
                schema: "identity",
                table: "users");
        }
    }
}
