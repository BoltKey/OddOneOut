using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class guesses10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_CardSet_CardSetId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_WordCard_OddOneOutId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Guesses_Games_GameId",
                table: "Guesses");

            migrationBuilder.DropForeignKey(
                name: "FK_Guesses_WordCard_SelectedCardId",
                table: "Guesses");

            migrationBuilder.DropColumn(
                name: "IsCorrect",
                table: "Guesses");

            migrationBuilder.AlterColumn<string>(
                name: "Word",
                table: "WordCard",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "WordCard",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "SelectedCardId",
                table: "Guesses",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "GameId",
                table: "Guesses",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "OddOneOutId",
                table: "Games",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Clue",
                table: "Games",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "CardSetId",
                table: "Games",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_CardSet_CardSetId",
                table: "Games",
                column: "CardSetId",
                principalTable: "CardSet",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_WordCard_OddOneOutId",
                table: "Games",
                column: "OddOneOutId",
                principalTable: "WordCard",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Guesses_Games_GameId",
                table: "Guesses",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Guesses_WordCard_SelectedCardId",
                table: "Guesses",
                column: "SelectedCardId",
                principalTable: "WordCard",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_CardSet_CardSetId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_WordCard_OddOneOutId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Guesses_Games_GameId",
                table: "Guesses");

            migrationBuilder.DropForeignKey(
                name: "FK_Guesses_WordCard_SelectedCardId",
                table: "Guesses");

            migrationBuilder.AlterColumn<string>(
                name: "Word",
                table: "WordCard",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "WordCard",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SelectedCardId",
                table: "Guesses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GameId",
                table: "Guesses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "Guesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "OddOneOutId",
                table: "Games",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Clue",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CardSetId",
                table: "Games",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_CardSet_CardSetId",
                table: "Games",
                column: "CardSetId",
                principalTable: "CardSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_WordCard_OddOneOutId",
                table: "Games",
                column: "OddOneOutId",
                principalTable: "WordCard",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Guesses_Games_GameId",
                table: "Guesses",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Guesses_WordCard_SelectedCardId",
                table: "Guesses",
                column: "SelectedCardId",
                principalTable: "WordCard",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
