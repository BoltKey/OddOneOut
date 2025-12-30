using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class gamecardsnn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WordCard_CardSet_CardSetId",
                table: "WordCard");

            migrationBuilder.DropIndex(
                name: "IX_WordCard_CardSetId",
                table: "WordCard");

            migrationBuilder.DropColumn(
                name: "CardSetId",
                table: "WordCard");

            migrationBuilder.CreateTable(
                name: "CardSetWordCard",
                columns: table => new
                {
                    CardSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    WordCardsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardSetWordCard", x => new { x.CardSetId, x.WordCardsId });
                    table.ForeignKey(
                        name: "FK_CardSetWordCard_CardSet_CardSetId",
                        column: x => x.CardSetId,
                        principalTable: "CardSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardSetWordCard_WordCard_WordCardsId",
                        column: x => x.WordCardsId,
                        principalTable: "WordCard",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardSetWordCard_WordCardsId",
                table: "CardSetWordCard",
                column: "WordCardsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardSetWordCard");

            migrationBuilder.AddColumn<Guid>(
                name: "CardSetId",
                table: "WordCard",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WordCard_CardSetId",
                table: "WordCard",
                column: "CardSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_WordCard_CardSet_CardSetId",
                table: "WordCard",
                column: "CardSetId",
                principalTable: "CardSet",
                principalColumn: "Id");
        }
    }
}
