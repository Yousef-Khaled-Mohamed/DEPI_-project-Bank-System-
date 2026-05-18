using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankSystemBackend.Migrations
{
    /// <inheritdoc />
    public partial class FixTransferTargetAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetAccountFK",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TargetAccountFK",
                table: "Transactions",
                column: "TargetAccountFK");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Account_TargetAccountFK",
                table: "Transactions",
                column: "TargetAccountFK",
                principalTable: "Account",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Account_TargetAccountFK",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_TargetAccountFK",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TargetAccountFK",
                table: "Transactions");
        }
    }
}
