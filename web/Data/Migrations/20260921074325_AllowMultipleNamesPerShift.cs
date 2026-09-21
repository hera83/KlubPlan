using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleNamesPerShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ArrangementRegistrationShifts",
                table: "ArrangementRegistrationShifts");

            migrationBuilder.AddColumn<bool>(
                name: "AllowMultipleNamesPerShift",
                table: "Arrangements",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ArrangementRegistrationShifts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "CompanionName",
                table: "ArrangementRegistrationShifts",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArrangementRegistrationShifts",
                table: "ArrangementRegistrationShifts",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ArrangementRegistrationShifts_ArrangementRegistrationId",
                table: "ArrangementRegistrationShifts",
                column: "ArrangementRegistrationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ArrangementRegistrationShifts",
                table: "ArrangementRegistrationShifts");

            migrationBuilder.DropIndex(
                name: "IX_ArrangementRegistrationShifts_ArrangementRegistrationId",
                table: "ArrangementRegistrationShifts");

            migrationBuilder.DropColumn(
                name: "AllowMultipleNamesPerShift",
                table: "Arrangements");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ArrangementRegistrationShifts");

            migrationBuilder.DropColumn(
                name: "CompanionName",
                table: "ArrangementRegistrationShifts");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArrangementRegistrationShifts",
                table: "ArrangementRegistrationShifts",
                columns: new[] { "ArrangementRegistrationId", "ArrangementShiftId" });
        }
    }
}
