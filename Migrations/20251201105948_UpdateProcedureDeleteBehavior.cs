using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMGC.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProcedureDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Procedures_ProcedureId",
                table: "Prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Procedures_ProcedureId",
                table: "Transactions");

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Procedures_ProcedureId",
                table: "Prescriptions",
                column: "ProcedureId",
                principalTable: "Procedures",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Procedures_ProcedureId",
                table: "Transactions",
                column: "ProcedureId",
                principalTable: "Procedures",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Procedures_ProcedureId",
                table: "Prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Procedures_ProcedureId",
                table: "Transactions");

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Procedures_ProcedureId",
                table: "Prescriptions",
                column: "ProcedureId",
                principalTable: "Procedures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Procedures_ProcedureId",
                table: "Transactions",
                column: "ProcedureId",
                principalTable: "Procedures",
                principalColumn: "Id");
        }
    }
}
