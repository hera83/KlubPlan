using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveActivityArrangementLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Arrangements_ArrangementId",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_ArrangementId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ArrangementId",
                table: "Activities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArrangementId",
                table: "Activities",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ArrangementId",
                table: "Activities",
                column: "ArrangementId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Arrangements_ArrangementId",
                table: "Activities",
                column: "ArrangementId",
                principalTable: "Arrangements",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
