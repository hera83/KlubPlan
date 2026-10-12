using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArrangements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Arrangements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    RegistrationOpensAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RegistrationClosesAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AccessMode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arrangements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArrangementAllowedGroups",
                columns: table => new
                {
                    ArrangementId = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonGroupId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArrangementAllowedGroups", x => new { x.ArrangementId, x.PersonGroupId });
                    table.ForeignKey(
                        name: "FK_ArrangementAllowedGroups_Arrangements_ArrangementId",
                        column: x => x.ArrangementId,
                        principalTable: "Arrangements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArrangementAllowedGroups_PersonGroups_PersonGroupId",
                        column: x => x.PersonGroupId,
                        principalTable: "PersonGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArrangementAllowedPersons",
                columns: table => new
                {
                    ArrangementId = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArrangementAllowedPersons", x => new { x.ArrangementId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_ArrangementAllowedPersons_Arrangements_ArrangementId",
                        column: x => x.ArrangementId,
                        principalTable: "Arrangements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArrangementAllowedPersons_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArrangementFormFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArrangementId = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    HelpText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FieldType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    OptionsJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArrangementFormFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArrangementFormFields_Arrangements_ArrangementId",
                        column: x => x.ArrangementId,
                        principalTable: "Arrangements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArrangementShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArrangementId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    NeededCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArrangementShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArrangementShifts_Arrangements_ArrangementId",
                        column: x => x.ArrangementId,
                        principalTable: "Arrangements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArrangementShiftRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArrangementShiftId = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArrangementShiftRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArrangementShiftRequirements_ArrangementShifts_ArrangementShiftId",
                        column: x => x.ArrangementShiftId,
                        principalTable: "ArrangementShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArrangementAllowedGroups_PersonGroupId",
                table: "ArrangementAllowedGroups",
                column: "PersonGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ArrangementAllowedPersons_PersonId",
                table: "ArrangementAllowedPersons",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ArrangementFormFields_ArrangementId_Order",
                table: "ArrangementFormFields",
                columns: new[] { "ArrangementId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_Arrangements_CreatedAtUtc",
                table: "Arrangements",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ArrangementShiftRequirements_ArrangementShiftId_Order",
                table: "ArrangementShiftRequirements",
                columns: new[] { "ArrangementShiftId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_ArrangementShifts_ArrangementId_Order",
                table: "ArrangementShifts",
                columns: new[] { "ArrangementId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_ArrangementShifts_StartUtc",
                table: "ArrangementShifts",
                column: "StartUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArrangementAllowedGroups");

            migrationBuilder.DropTable(
                name: "ArrangementAllowedPersons");

            migrationBuilder.DropTable(
                name: "ArrangementFormFields");

            migrationBuilder.DropTable(
                name: "ArrangementShiftRequirements");

            migrationBuilder.DropTable(
                name: "ArrangementShifts");

            migrationBuilder.DropTable(
                name: "Arrangements");
        }
    }
}
