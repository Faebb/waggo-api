using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Waggo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWalkMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "messaging");

            migrationBuilder.CreateTable(
                name: "walk_messages",
                schema: "messaging",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    walk_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sent_by = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_walk_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_walk_messages_walk_id_sent_at",
                schema: "messaging",
                table: "walk_messages",
                columns: new[] { "walk_id", "sent_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "walk_messages",
                schema: "messaging");
        }
    }
}
