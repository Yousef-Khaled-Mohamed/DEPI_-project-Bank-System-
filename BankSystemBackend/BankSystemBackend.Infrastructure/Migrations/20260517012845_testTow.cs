using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankSystemBackend.Migrations
{
    /// <inheritdoc />
    public partial class testTow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Term",
                table: "Loans",
                newName: "DurationMonths");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Loans",
                newName: "RemainingAmount");

            migrationBuilder.AddColumn<double>(
                name: "OriginalAmount",
                table: "Loans",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Loans",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Loans");

            migrationBuilder.RenameColumn(
                name: "RemainingAmount",
                table: "Loans",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "DurationMonths",
                table: "Loans",
                newName: "Term");
        }
    }
}
