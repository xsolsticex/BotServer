using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotServer.Migrations
{
    /// <inheritdoc />
    public partial class ChannelBadgesConstraintUpdatedNew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GlobalBadges_name",
                table: "GlobalBadges");

            migrationBuilder.AddColumn<string>(
                name: "BadgeId",
                table: "GlobalBadges",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalBadges_BadgeId",
                table: "GlobalBadges",
                column: "BadgeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GlobalBadges_BadgeId",
                table: "GlobalBadges");

            migrationBuilder.DropColumn(
                name: "BadgeId",
                table: "GlobalBadges");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalBadges_name",
                table: "GlobalBadges",
                column: "name",
                unique: true);
        }
    }
}
