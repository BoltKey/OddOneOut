using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class gamecards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameWordCard");

            migrationBuilder.DropColumn(
                name: "GameStateJson",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Player1Name",
                table: "Games");

            migrationBuilder.AddColumn<Guid>(
                name: "CardSetId",
                table: "WordCard",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CardSetId",
                table: "Games",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<List<string>>(
                name: "ClueGiver",
                table: "Games",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OddOneOutId",
                table: "Games",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "CardSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardSet", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WordCard_CardSetId",
                table: "WordCard",
                column: "CardSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_CardSetId",
                table: "Games",
                column: "CardSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_OddOneOutId",
                table: "Games",
                column: "OddOneOutId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_CardSet_CardSetId",
                table: "Games",
                column: "CardSetId",
                principalTable: "CardSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_WordCard_OddOneOutId",
                table: "Games",
                column: "OddOneOutId",
                principalTable: "WordCard",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WordCard_CardSet_CardSetId",
                table: "WordCard",
                column: "CardSetId",
                principalTable: "CardSet",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_CardSet_CardSetId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_WordCard_OddOneOutId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_WordCard_CardSet_CardSetId",
                table: "WordCard");

            migrationBuilder.DropTable(
                name: "CardSet");

            migrationBuilder.DropIndex(
                name: "IX_WordCard_CardSetId",
                table: "WordCard");

            migrationBuilder.DropIndex(
                name: "IX_Games_CardSetId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_OddOneOutId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "CardSetId",
                table: "WordCard");

            migrationBuilder.DropColumn(
                name: "CardSetId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ClueGiver",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "OddOneOutId",
                table: "Games");

            migrationBuilder.AddColumn<string>(
                name: "GameStateJson",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Player1Name",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");

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
    }
}
