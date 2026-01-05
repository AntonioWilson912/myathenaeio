using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAthenaeio.Migrations
{
    /// <inheritdoc />
    public partial class AddBookCopy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Copies",
                table: "Books");

            migrationBuilder.AddColumn<int>(
                name: "BookCopyId",
                table: "Loans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Collections",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Borrowers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateAdded",
                table: "Books",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "BookCopies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookId = table.Column<int>(type: "INTEGER", nullable: false),
                    CopyNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AcquisitionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookCopies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookCopies_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_BookCopyId",
                table: "Loans",
                column: "BookCopyId");

            migrationBuilder.CreateIndex(
                name: "IX_BookCopies_BookId",
                table: "BookCopies",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BookCopies_BookId_CopyNumber",
                table: "BookCopies",
                columns: new[] { "BookId", "CopyNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Loans_BookCopies_BookCopyId",
                table: "Loans",
                column: "BookCopyId",
                principalTable: "BookCopies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Loans_BookCopies_BookCopyId",
                table: "Loans");

            migrationBuilder.DropTable(
                name: "BookCopies");

            migrationBuilder.DropIndex(
                name: "IX_Loans_BookCopyId",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "BookCopyId",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Borrowers");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateAdded",
                table: "Books",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "Copies",
                table: "Books",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
