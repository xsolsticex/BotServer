using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotServer.Migrations
{
    /// <inheritdoc />
    public partial class AddNewDbTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomBadges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<int>(type: "INTEGER", nullable: false),
                    SetId = table.Column<string>(type: "TEXT", nullable: false),
                    BadgeUrl = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomBadges_JoinedChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "JoinedChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomBadges_BadgeUrl",
                table: "CustomBadges",
                column: "BadgeUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomBadges_ChannelId",
                table: "CustomBadges",
                column: "ChannelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomBadges");
        }
    }
}
