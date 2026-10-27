using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Waggo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConfirmationAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_confirmation_token_hash",
                schema: "identity",
                table: "users");

            migrationBuilder.AddColumn<int>(
                name: "confirmation_attempts",
                schema: "identity",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "confirmation_attempts",
                schema: "identity",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ix_users_confirmation_token_hash",
                schema: "identity",
                table: "users",
                column: "confirmation_token_hash");
        }
    }
}
