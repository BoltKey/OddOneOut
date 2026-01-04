using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class cluetime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameClueGivers_AspNetUsers_ClueGiversId",
                table: "GameClueGivers");

            migrationBuilder.DropForeignKey(
                name: "FK_GameClueGivers_Games_CreatedGamesId",
                table: "GameClueGivers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameClueGivers",
                table: "GameClueGivers");

            migrationBuilder.DropIndex(
                name: "IX_GameClueGivers_CreatedGamesId",
                table: "GameClueGivers");

            migrationBuilder.RenameColumn(
                name: "CreatedGamesId",
                table: "GameClueGivers",
                newName: "GameId");

            migrationBuilder.RenameColumn(
                name: "ClueGiversId",
                table: "GameClueGivers",
                newName: "UserId");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClueGivenAt",
                table: "GameClueGivers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameClueGivers",
                table: "GameClueGivers",
                columns: new[] { "GameId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Guesses_GuessedAt",
                table: "Guesses",
                column: "GuessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GameClueGivers_ClueGivenAt",
                table: "GameClueGivers",
                column: "ClueGivenAt");

            migrationBuilder.CreateIndex(
                name: "IX_GameClueGivers_UserId",
                table: "GameClueGivers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_GameClueGivers_AspNetUsers_UserId",
                table: "GameClueGivers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameClueGivers_Games_GameId",
                table: "GameClueGivers",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameClueGivers_AspNetUsers_UserId",
                table: "GameClueGivers");

            migrationBuilder.DropForeignKey(
                name: "FK_GameClueGivers_Games_GameId",
                table: "GameClueGivers");

            migrationBuilder.DropIndex(
                name: "IX_Guesses_GuessedAt",
                table: "Guesses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameClueGivers",
                table: "GameClueGivers");

            migrationBuilder.DropIndex(
                name: "IX_GameClueGivers_ClueGivenAt",
                table: "GameClueGivers");

            migrationBuilder.DropIndex(
                name: "IX_GameClueGivers_UserId",
                table: "GameClueGivers");

            migrationBuilder.DropColumn(
                name: "ClueGivenAt",
                table: "GameClueGivers");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "GameClueGivers",
                newName: "ClueGiversId");

            migrationBuilder.RenameColumn(
                name: "GameId",
                table: "GameClueGivers",
                newName: "CreatedGamesId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameClueGivers",
                table: "GameClueGivers",
                columns: new[] { "ClueGiversId", "CreatedGamesId" });

            migrationBuilder.CreateIndex(
                name: "IX_GameClueGivers_CreatedGamesId",
                table: "GameClueGivers",
                column: "CreatedGamesId");

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
        }
    }
}
