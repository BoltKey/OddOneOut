using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class guesses9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guesses_AspNetUsers_UserId",
                table: "Guesses");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "Guesses");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Guesses",
                newName: "GuesserId");

            migrationBuilder.RenameIndex(
                name: "IX_Guesses_UserId",
                table: "Guesses",
                newName: "IX_Guesses_GuesserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Guesses_AspNetUsers_GuesserId",
                table: "Guesses",
                column: "GuesserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guesses_AspNetUsers_GuesserId",
                table: "Guesses");

            migrationBuilder.RenameColumn(
                name: "GuesserId",
                table: "Guesses",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Guesses_GuesserId",
                table: "Guesses",
                newName: "IX_Guesses_UserId");

            migrationBuilder.AddColumn<string>(
                name: "PlayerId",
                table: "Guesses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Guesses_AspNetUsers_UserId",
                table: "Guesses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
