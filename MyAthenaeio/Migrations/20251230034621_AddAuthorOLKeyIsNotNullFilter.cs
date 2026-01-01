using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAthenaeio.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorOLKeyIsNotNullFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Authors_OpenLibraryKey",
                table: "Authors");

            migrationBuilder.CreateIndex(
                name: "IX_Authors_OpenLibraryKey",
                table: "Authors",
                column: "OpenLibraryKey",
                unique: true,
                filter: "OpenLibraryKey IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Authors_OpenLibraryKey",
                table: "Authors");

            migrationBuilder.CreateIndex(
                name: "IX_Authors_OpenLibraryKey",
                table: "Authors",
                column: "OpenLibraryKey",
                unique: true);
        }
    }
}
