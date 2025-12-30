using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class guesses6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentCardId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CurrentCardId",
                table: "AspNetUsers",
                column: "CurrentCardId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_WordCard_CurrentCardId",
                table: "AspNetUsers",
                column: "CurrentCardId",
                principalTable: "WordCard",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_WordCard_CurrentCardId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CurrentCardId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CurrentCardId",
                table: "AspNetUsers");
        }
    }
}
