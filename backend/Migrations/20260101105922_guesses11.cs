using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class guesses11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "CardSet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GuessRating",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "CardSet");

            migrationBuilder.DropColumn(
                name: "GuessRating",
                table: "AspNetUsers");
        }
    }
}
