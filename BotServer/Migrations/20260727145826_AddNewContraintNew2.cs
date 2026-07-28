using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotServer.Migrations
{
    /// <inheritdoc />
    public partial class AddNewContraintNew2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GlobalBadges_url",
                table: "GlobalBadges");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GlobalBadges_url",
                table: "GlobalBadges",
                column: "url",
                unique: true);
        }
    }
}
