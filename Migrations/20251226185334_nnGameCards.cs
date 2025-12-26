using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class nnGameCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WordCard_Games_GameId",
                table: "WordCard");

            migrationBuilder.DropIndex(
                name: "IX_WordCard_GameId",
                table: "WordCard");

            migrationBuilder.DropColumn(
                name: "GameId",
                table: "WordCard");

            migrationBuilder.CreateTable(
                name: "GameWordCard",
                columns: table => new
                {
                    GamesId = table.Column<Guid>(type: "uuid", nullable: false),
                    WordCardsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameWordCard", x => new { x.GamesId, x.WordCardsId });
                    table.ForeignKey(
                        name: "FK_GameWordCard_Games_GamesId",
                        column: x => x.GamesId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameWordCard_WordCard_WordCardsId",
                        column: x => x.WordCardsId,
                        principalTable: "WordCard",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameWordCard_WordCardsId",
                table: "GameWordCard",
                column: "WordCardsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameWordCard");

            migrationBuilder.AddColumn<Guid>(
                name: "GameId",
                table: "WordCard",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WordCard_GameId",
                table: "WordCard",
                column: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_WordCard_Games_GameId",
                table: "WordCard",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id");
        }
    }
}
