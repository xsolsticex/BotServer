using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotServer.Data.Migrations;

public partial class CustomBadgeIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_CustomBadges_BadgeUrl", "CustomBadges");
        migrationBuilder.DropIndex("IX_CustomBadges_ChannelId", "CustomBadges");

        // Old imports used display titles and allowed duplicate SetIds within a channel.
        // Preserve every row and URL, keeping the newest row under its original key.
        // Older duplicate rows retain their title in an unambiguous legacy key.
        migrationBuilder.Sql("""
            UPDATE "CustomBadges"
            SET "SetId" = 'legacy:' || "Id" || ':' || "SetId"
            WHERE "Id" NOT IN (
                SELECT MAX("Id") FROM "CustomBadges"
                GROUP BY "ChannelId", lower(trim("SetId"))
            );
            UPDATE "CustomBadges" SET "SetId" = lower(trim("SetId"));
            """);
        migrationBuilder.CreateIndex(
            name: "IX_CustomBadges_ChannelId_SetId",
            table: "CustomBadges",
            columns: new[] { "ChannelId", "SetId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_CustomBadges_ChannelId_SetId", "CustomBadges");
        migrationBuilder.CreateIndex(
            name: "IX_CustomBadges_ChannelId", table: "CustomBadges", column: "ChannelId");
        // SQLite refuses rollback if URLs have since been shared; never delete data to force it.
        migrationBuilder.CreateIndex(
            name: "IX_CustomBadges_BadgeUrl", table: "CustomBadges",
            column: "BadgeUrl", unique: true);
    }
}
