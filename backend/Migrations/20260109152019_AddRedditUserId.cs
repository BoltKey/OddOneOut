using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class AddRedditUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RedditUserId",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_RedditUserId",
                table: "AspNetUsers",
                column: "RedditUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_RedditUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RedditUserId",
                table: "AspNetUsers");
        }
    }
}
