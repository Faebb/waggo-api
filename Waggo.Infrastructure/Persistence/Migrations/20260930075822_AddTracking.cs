using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Waggo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tracking");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "finished_at",
                schema: "walks",
                table: "walks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "started_at",
                schema: "walks",
                table: "walks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "track_points",
                schema: "tracking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    walk_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location = table.Column<Point>(type: "geography (point, 4326)", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_track_points", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_track_points_location",
                schema: "tracking",
                table: "track_points",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_track_points_recorded_at",
                schema: "tracking",
                table: "track_points",
                column: "recorded_at")
                .Annotation("Npgsql:IndexMethod", "brin");

            migrationBuilder.CreateIndex(
                name: "ix_track_points_walk_id",
                schema: "tracking",
                table: "track_points",
                column: "walk_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "track_points",
                schema: "tracking");

            migrationBuilder.DropColumn(
                name: "finished_at",
                schema: "walks",
                table: "walks");

            migrationBuilder.DropColumn(
                name: "started_at",
                schema: "walks",
                table: "walks");
        }
    }
}
