using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivityId",
                table: "CommunicationMessages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StartAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsCancelled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArrangementId = table.Column<int>(type: "INTEGER", nullable: true),
                    FormId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_Arrangements_ArrangementId",
                        column: x => x.ArrangementId,
                        principalTable: "Arrangements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Activities_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ActivityTargetGroups",
                columns: table => new
                {
                    ActivityId = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonGroupId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTargetGroups", x => new { x.ActivityId, x.PersonGroupId });
                    table.ForeignKey(
                        name: "FK_ActivityTargetGroups_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityTargetGroups_PersonGroups_PersonGroupId",
                        column: x => x.PersonGroupId,
                        principalTable: "PersonGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityWorkgroupMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Mobile = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityWorkgroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityWorkgroupMembers_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DeadlineAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssignedToWorkgroupMemberId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityTasks_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityTasks_ActivityWorkgroupMembers_AssignedToWorkgroupMemberId",
                        column: x => x.AssignedToWorkgroupMemberId,
                        principalTable: "ActivityWorkgroupMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessages_ActivityId",
                table: "CommunicationMessages",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ArrangementId",
                table: "Activities",
                column: "ArrangementId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_CreatedAtUtc",
                table: "Activities",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_FormId",
                table: "Activities",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_StartAtUtc",
                table: "Activities",
                column: "StartAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTargetGroups_PersonGroupId",
                table: "ActivityTargetGroups",
                column: "PersonGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTasks_ActivityId_Order",
                table: "ActivityTasks",
                columns: new[] { "ActivityId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTasks_AssignedToWorkgroupMemberId",
                table: "ActivityTasks",
                column: "AssignedToWorkgroupMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTasks_DeadlineAtUtc",
                table: "ActivityTasks",
                column: "DeadlineAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityWorkgroupMembers_ActivityId_Order",
                table: "ActivityWorkgroupMembers",
                columns: new[] { "ActivityId", "Order" });

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationMessages_Activities_ActivityId",
                table: "CommunicationMessages",
                column: "ActivityId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationMessages_Activities_ActivityId",
                table: "CommunicationMessages");

            migrationBuilder.DropTable(
                name: "ActivityTargetGroups");

            migrationBuilder.DropTable(
                name: "ActivityTasks");

            migrationBuilder.DropTable(
                name: "ActivityWorkgroupMembers");

            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationMessages_ActivityId",
                table: "CommunicationMessages");

            migrationBuilder.DropColumn(
                name: "ActivityId",
                table: "CommunicationMessages");
        }
    }
}
