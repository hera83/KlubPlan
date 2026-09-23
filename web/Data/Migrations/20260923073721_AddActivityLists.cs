using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    SourceFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ShowNote = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLists_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityListColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityListId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Options = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityListColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityListColumns_ActivityLists_ActivityListId",
                        column: x => x.ActivityListId,
                        principalTable: "ActivityLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityListStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityListId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityListStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityListStatuses_ActivityLists_ActivityListId",
                        column: x => x.ActivityListId,
                        principalTable: "ActivityLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityListItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityListId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    StatusId = table.Column<int>(type: "INTEGER", nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    AssignedToWorkgroupMemberId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityListItems_ActivityListStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "ActivityListStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActivityListItems_ActivityLists_ActivityListId",
                        column: x => x.ActivityListId,
                        principalTable: "ActivityLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityListItems_ActivityWorkgroupMembers_AssignedToWorkgroupMemberId",
                        column: x => x.AssignedToWorkgroupMemberId,
                        principalTable: "ActivityWorkgroupMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActivityListItems_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ActivityListCellValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityListItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityListColumnId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityListCellValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityListCellValues_ActivityListColumns_ActivityListColumnId",
                        column: x => x.ActivityListColumnId,
                        principalTable: "ActivityListColumns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityListCellValues_ActivityListItems_ActivityListItemId",
                        column: x => x.ActivityListItemId,
                        principalTable: "ActivityListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityListCellValues_ActivityListColumnId",
                table: "ActivityListCellValues",
                column: "ActivityListColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityListCellValues_ActivityListItemId_ActivityListColumnId",
                table: "ActivityListCellValues",
                columns: new[] { "ActivityListItemId", "ActivityListColumnId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityListColumns_ActivityListId_Order",
                table: "ActivityListColumns",
                columns: new[] { "ActivityListId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityListItems_ActivityListId_Order",
                table: "ActivityListItems",
                columns: new[] { "ActivityListId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityListItems_AssignedToWorkgroupMemberId",
                table: "ActivityListItems",
                column: "AssignedToWorkgroupMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityListItems_StatusId",
                table: "ActivityListItems",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityListItems_UpdatedByUserId",
                table: "ActivityListItems",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLists_ActivityId",
                table: "ActivityLists",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityListStatuses_ActivityListId_Order",
                table: "ActivityListStatuses",
                columns: new[] { "ActivityListId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityListCellValues");

            migrationBuilder.DropTable(
                name: "ActivityListColumns");

            migrationBuilder.DropTable(
                name: "ActivityListItems");

            migrationBuilder.DropTable(
                name: "ActivityListStatuses");

            migrationBuilder.DropTable(
                name: "ActivityLists");
        }
    }
}
