using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityForms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityForms",
                columns: table => new
                {
                    ActivityId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityForms", x => new { x.ActivityId, x.FormId });
                    table.ForeignKey(
                        name: "FK_ActivityForms_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityForms_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityForms_FormId",
                table: "ActivityForms",
                column: "FormId");

            // Backfill: eksisterende single-form-links flyttes til join-tabellen før kolonnen droppes.
            migrationBuilder.Sql(
                "INSERT INTO ActivityForms (ActivityId, FormId) SELECT Id, FormId FROM Activities WHERE FormId IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Forms_FormId",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_FormId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "FormId",
                table: "Activities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FormId",
                table: "Activities",
                type: "INTEGER",
                nullable: true);

            // Kolonnen kan kun rumme ét link pr. aktivitet — tag ét vilkårligt link pr. aktivitet ved rollback.
            migrationBuilder.Sql(
                "UPDATE Activities SET FormId = (SELECT FormId FROM ActivityForms WHERE ActivityForms.ActivityId = Activities.Id LIMIT 1);");

            migrationBuilder.DropTable(
                name: "ActivityForms");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_FormId",
                table: "Activities",
                column: "FormId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Forms_FormId",
                table: "Activities",
                column: "FormId",
                principalTable: "Forms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
