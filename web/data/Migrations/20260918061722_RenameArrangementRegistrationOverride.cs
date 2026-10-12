using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameArrangementRegistrationOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegistrationManuallyOpen",
                table: "Arrangements");

            migrationBuilder.AddColumn<bool>(
                name: "RegistrationForcedOpen",
                table: "Arrangements",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegistrationForcedOpen",
                table: "Arrangements");

            migrationBuilder.AddColumn<bool>(
                name: "RegistrationManuallyOpen",
                table: "Arrangements",
                type: "INTEGER",
                nullable: true);
        }
    }
}
