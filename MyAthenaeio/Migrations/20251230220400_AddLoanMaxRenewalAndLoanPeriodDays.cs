using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAthenaeio.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanMaxRenewalAndLoanPeriodDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OldDueDate",
                table: "Renewals",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "LoanPeriodDays",
                table: "Loans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 14);

            migrationBuilder.AddColumn<int>(
                name: "MaxRenewalsAllowed",
                table: "Loans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OldDueDate",
                table: "Renewals");

            migrationBuilder.DropColumn(
                name: "LoanPeriodDays",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "MaxRenewalsAllowed",
                table: "Loans");
        }
    }
}
