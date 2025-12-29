using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class guesses5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_CardSet_assignedCardSetId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_GameUser_AspNetUsers_ClueGiverId",
                table: "GameUser");

            migrationBuilder.DropForeignKey(
                name: "FK_GameUser_Games_CreatedGamesId",
                table: "GameUser");

            migrationBuilder.DropForeignKey(
                name: "FK_Guess_AspNetUsers_UserId",
                table: "Guess");

            migrationBuilder.DropForeignKey(
                name: "FK_Guess_Games_GameId",
                table: "Guess");

            migrationBuilder.DropForeignKey(
                name: "FK_Guess_WordCard_SelectedCardId",
                table: "Guess");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Guess",
                table: "Guess");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameUser",
                table: "GameUser");

            migrationBuilder.RenameTable(
                name: "Guess",
                newName: "Guesses");

            migrationBuilder.RenameTable(
                name: "GameUser",
                newName: "GameClueGivers");

            migrationBuilder.RenameColumn(
                name: "assignedCardSetId",
                table: "AspNetUsers",
                newName: "AssignedCardSetId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_assignedCardSetId",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_AssignedCardSetId");

            migrationBuilder.RenameIndex(
                name: "IX_Guess_UserId",
                table: "Guesses",
                newName: "IX_Guesses_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Guess_SelectedCardId",
                table: "Guesses",
                newName: "IX_Guesses_SelectedCardId");

            migrationBuilder.RenameIndex(
                name: "IX_Guess_GameId",
                table: "Guesses",
                newName: "IX_Guesses_GameId");

            migrationBuilder.RenameColumn(
                name: "ClueGiverId",
                table: "GameClueGivers",
                newName: "ClueGiversId");

            migrationBuilder.RenameIndex(
                name: "IX_GameUser_CreatedGamesId",
                table: "GameClueGivers",
                newName: "IX_GameClueGivers_CreatedGamesId");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentGameId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Guesses",
                table: "Guesses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameClueGivers",
                table: "GameClueGivers",
                columns: new[] { "ClueGiversId", "CreatedGamesId" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CurrentGameId",
                table: "AspNetUsers",
                column: "CurrentGameId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_CardSet_AssignedCardSetId",
                table: "AspNetUsers",
                column: "AssignedCardSetId",
                principalTable: "CardSet",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Games_CurrentGameId",
                table: "AspNetUsers",
                column: "CurrentGameId",
                principalTable: "Games",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GameClueGivers_AspNetUsers_ClueGiversId",
                table: "GameClueGivers",
                column: "ClueGiversId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameClueGivers_Games_CreatedGamesId",
                table: "GameClueGivers",
                column: "CreatedGamesId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Guesses_AspNetUsers_UserId",
                table: "Guesses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Guesses_Games_GameId",
                table: "Guesses",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Guesses_WordCard_SelectedCardId",
                table: "Guesses",
                column: "SelectedCardId",
                principalTable: "WordCard",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_CardSet_AssignedCardSetId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Games_CurrentGameId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_GameClueGivers_AspNetUsers_ClueGiversId",
                table: "GameClueGivers");

            migrationBuilder.DropForeignKey(
                name: "FK_GameClueGivers_Games_CreatedGamesId",
                table: "GameClueGivers");

            migrationBuilder.DropForeignKey(
                name: "FK_Guesses_AspNetUsers_UserId",
                table: "Guesses");

            migrationBuilder.DropForeignKey(
                name: "FK_Guesses_Games_GameId",
                table: "Guesses");

            migrationBuilder.DropForeignKey(
                name: "FK_Guesses_WordCard_SelectedCardId",
                table: "Guesses");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CurrentGameId",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Guesses",
                table: "Guesses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameClueGivers",
                table: "GameClueGivers");

            migrationBuilder.DropColumn(
                name: "CurrentGameId",
                table: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "Guesses",
                newName: "Guess");

            migrationBuilder.RenameTable(
                name: "GameClueGivers",
                newName: "GameUser");

            migrationBuilder.RenameColumn(
                name: "AssignedCardSetId",
                table: "AspNetUsers",
                newName: "assignedCardSetId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_AssignedCardSetId",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_assignedCardSetId");

            migrationBuilder.RenameIndex(
                name: "IX_Guesses_UserId",
                table: "Guess",
                newName: "IX_Guess_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Guesses_SelectedCardId",
                table: "Guess",
                newName: "IX_Guess_SelectedCardId");

            migrationBuilder.RenameIndex(
                name: "IX_Guesses_GameId",
                table: "Guess",
                newName: "IX_Guess_GameId");

            migrationBuilder.RenameColumn(
                name: "ClueGiversId",
                table: "GameUser",
                newName: "ClueGiverId");

            migrationBuilder.RenameIndex(
                name: "IX_GameClueGivers_CreatedGamesId",
                table: "GameUser",
                newName: "IX_GameUser_CreatedGamesId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Guess",
                table: "Guess",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameUser",
                table: "GameUser",
                columns: new[] { "ClueGiverId", "CreatedGamesId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_CardSet_assignedCardSetId",
                table: "AspNetUsers",
                column: "assignedCardSetId",
                principalTable: "CardSet",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GameUser_AspNetUsers_ClueGiverId",
                table: "GameUser",
                column: "ClueGiverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameUser_Games_CreatedGamesId",
                table: "GameUser",
                column: "CreatedGamesId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Guess_AspNetUsers_UserId",
                table: "Guess",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Guess_Games_GameId",
                table: "Guess",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Guess_WordCard_SelectedCardId",
                table: "Guess",
                column: "SelectedCardId",
                principalTable: "WordCard",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
