using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Waggo.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Bootstrap: production has no way to promote a user to admin other than a direct database write
    /// (<c>User.IsAdmin</c> is seeded by hand, see its remarks). Promotes the earliest-registered account only if
    /// no admin exists yet, so re-running this (or applying it again elsewhere) is a no-op once one does.
    /// </summary>
    public partial class SeedFirstAdminIfNone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE identity.users
                SET is_admin = TRUE
                WHERE id = (SELECT id FROM identity.users ORDER BY registered_at LIMIT 1)
                  AND NOT EXISTS (SELECT 1 FROM identity.users WHERE is_admin = TRUE);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally irreversible: reverting could demote whichever account holds the admin role by then,
            // not necessarily the one this migration promoted.
        }
    }
}
