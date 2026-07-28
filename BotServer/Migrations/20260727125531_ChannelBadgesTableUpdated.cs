using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotServer.Migrations
{
    /// <inheritdoc />
    public partial class ChannelBadgesTableUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalBadges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    url = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalBadges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GlobalBadges_name",
                table: "GlobalBadges",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalBadges");
        }
    }
}
