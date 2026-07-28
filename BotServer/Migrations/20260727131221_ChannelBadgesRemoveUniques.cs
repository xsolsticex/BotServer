using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotServer.Migrations
{
    /// <inheritdoc />
    public partial class ChannelBadgesRemoveUniques : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GlobalBadges_BadgeId",
                table: "GlobalBadges");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GlobalBadges_BadgeId",
                table: "GlobalBadges",
                column: "BadgeId",
                unique: true);
        }
    }
}
