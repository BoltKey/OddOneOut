using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class itch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ItchioUserId",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ItchioUserId",
                table: "AspNetUsers",
                column: "ItchioUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ItchioUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ItchioUserId",
                table: "AspNetUsers");
        }
    }
}
