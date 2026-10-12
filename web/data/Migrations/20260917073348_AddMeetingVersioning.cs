using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RootMeetingId",
                table: "Meetings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "Meetings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_RootMeetingId",
                table: "Meetings",
                column: "RootMeetingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Meetings_Meetings_RootMeetingId",
                table: "Meetings",
                column: "RootMeetingId",
                principalTable: "Meetings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meetings_Meetings_RootMeetingId",
                table: "Meetings");

            migrationBuilder.DropIndex(
                name: "IX_Meetings_RootMeetingId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "RootMeetingId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "Meetings");
        }
    }
}
