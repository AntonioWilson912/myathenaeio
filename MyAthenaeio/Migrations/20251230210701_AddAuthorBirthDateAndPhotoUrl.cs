using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAthenaeio.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorBirthDateAndPhotoUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "BookCopies",
                type: "TEXT",
                nullable: true,
                defaultValue: "New");

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "Authors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "Authors",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Condition",
                table: "BookCopies");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Authors");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "Authors");
        }
    }
}
