using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingGroupsManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeetingGroups",
                columns: table => new
                {
                    MeetingId = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonGroupId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingGroups", x => new { x.MeetingId, x.PersonGroupId });
                    table.ForeignKey(
                        name: "FK_MeetingGroups_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingGroups_PersonGroups_PersonGroupId",
                        column: x => x.PersonGroupId,
                        principalTable: "PersonGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingGroups_PersonGroupId",
                table: "MeetingGroups",
                column: "PersonGroupId");

            // Preserve each meeting's existing single group as a row in the new join table before dropping the column.
            migrationBuilder.Sql(
                "INSERT INTO MeetingGroups (MeetingId, PersonGroupId) SELECT Id, PersonGroupId FROM Meetings WHERE PersonGroupId IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_Meetings_PersonGroups_PersonGroupId",
                table: "Meetings");

            migrationBuilder.DropIndex(
                name: "IX_Meetings_PersonGroupId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "PersonGroupId",
                table: "Meetings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PersonGroupId",
                table: "Meetings",
                type: "INTEGER",
                nullable: true);

            // Best-effort restore: a meeting with several groups only keeps one (the lowest group id) since the old column allowed just one.
            migrationBuilder.Sql(
                "UPDATE Meetings SET PersonGroupId = (SELECT MIN(PersonGroupId) FROM MeetingGroups WHERE MeetingGroups.MeetingId = Meetings.Id);");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_PersonGroupId",
                table: "Meetings",
                column: "PersonGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Meetings_PersonGroups_PersonGroupId",
                table: "Meetings",
                column: "PersonGroupId",
                principalTable: "PersonGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropTable(
                name: "MeetingGroups");
        }
    }
}
