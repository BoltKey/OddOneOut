using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class _5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "CardSet");

            migrationBuilder.AddColumn<Guid>(
                name: "assignedCardSetId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_assignedCardSetId",
                table: "Users",
                column: "assignedCardSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_CardSet_assignedCardSetId",
                table: "Users",
                column: "assignedCardSetId",
                principalTable: "CardSet",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_CardSet_assignedCardSetId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_assignedCardSetId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "assignedCardSetId",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CardSet",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
