using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotServer.Migrations
{
    /// <inheritdoc />
    public partial class ChangeIdSTRtoINTValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelMessages_JoinedChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "JoinedChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMessages_ChannelId",
                table: "ChannelMessages",
                column: "ChannelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelMessages");
        }
    }
}
