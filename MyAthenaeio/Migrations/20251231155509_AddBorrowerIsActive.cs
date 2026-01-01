using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAthenaeio.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowerIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Borrowers",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Borrowers");
        }
    }
}
