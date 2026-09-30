using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Waggo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWalkConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "accepted_at",
                schema: "walks",
                table: "walks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "walks",
                table: "walks",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "ix_walks_walker_id",
                schema: "walks",
                table: "walks",
                column: "walker_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_walks_walker_id",
                schema: "walks",
                table: "walks");

            migrationBuilder.DropColumn(
                name: "accepted_at",
                schema: "walks",
                table: "walks");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "walks",
                table: "walks");
        }
    }
}
