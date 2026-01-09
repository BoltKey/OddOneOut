using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class gamedeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameClueGivers_AspNetUsers_UserId",
                table: "GameClueGivers");

            migrationBuilder.DropForeignKey(
                name: "FK_Guesses_AspNetUsers_GuesserId",
                table: "Guesses");

            migrationBuilder.DropForeignKey(
                name: "FK_Guesses_Games_GameId",
                table: "Guesses");

            migrationBuilder.AddForeignKey(
                name: "FK_GameClueGivers_AspNetUsers_UserId",
                table: "GameClueGivers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Guesses_AspNetUsers_GuesserId",
                table: "Guesses",
                column: "GuesserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Guesses_Games_GameId",
                table: "Guesses",
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
                name: "FK_Guesses_AspNetUsers_GuesserId",
                table: "Guesses");

            migrationBuilder.DropForeignKey(
                name: "FK_Guesses_Games_GameId",
                table: "Guesses");

            migrationBuilder.AddForeignKey(
                name: "FK_GameClueGivers_AspNetUsers_UserId",
                table: "GameClueGivers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Guesses_AspNetUsers_GuesserId",
                table: "Guesses",
                column: "GuesserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Guesses_Games_GameId",
                table: "Guesses",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id");
        }
    }
}
