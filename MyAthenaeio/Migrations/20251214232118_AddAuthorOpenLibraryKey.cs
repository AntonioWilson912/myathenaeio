using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAthenaeio.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorOpenLibraryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpenLibraryKey",
                table: "Authors",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Authors_OpenLibraryKey",
                table: "Authors",
                column: "OpenLibraryKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Authors_OpenLibraryKey",
                table: "Authors");

            migrationBuilder.DropColumn(
                name: "OpenLibraryKey",
                table: "Authors");
        }
    }
}
