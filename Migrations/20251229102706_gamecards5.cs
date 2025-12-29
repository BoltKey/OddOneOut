using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class gamecards5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Clue",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Clue",
                table: "Games");
        }
    }
}
