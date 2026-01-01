using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class guesses15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CachedClueRating",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Games_CachedGameScore",
                table: "Games",
                column: "CachedGameScore");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CachedClueRating",
                table: "AspNetUsers",
                column: "CachedClueRating");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GuessRating",
                table: "AspNetUsers",
                column: "GuessRating");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_CachedGameScore",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CachedClueRating",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_GuessRating",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CachedClueRating",
                table: "AspNetUsers");
        }
    }
}
