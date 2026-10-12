using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTilmeldingPublicLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArrangementId",
                table: "CommunicationMessages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Arrangements",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // Every existing row got the same empty default above — give each a distinct, random
            // id before the unique index below is created. Mirrors AddFormPublicId.
            migrationBuilder.Sql(@"
                UPDATE Arrangements
                SET PublicId =
                    lower(hex(randomblob(4))) || '-' ||
                    lower(hex(randomblob(2))) || '-' ||
                    lower(hex(randomblob(2))) || '-' ||
                    lower(hex(randomblob(2))) || '-' ||
                    lower(hex(randomblob(6)));
            ");

            migrationBuilder.CreateTable(
                name: "ArrangementRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArrangementId = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonId = table.Column<int>(type: "INTEGER", nullable: false),
                    RegisteredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArrangementRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArrangementRegistrations_Arrangements_ArrangementId",
                        column: x => x.ArrangementId,
                        principalTable: "Arrangements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArrangementRegistrations_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArrangementRegistrationAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArrangementRegistrationId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArrangementFormFieldId = table.Column<int>(type: "INTEGER", nullable: false),
                    ValueText = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArrangementRegistrationAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArrangementRegistrationAnswers_ArrangementFormFields_ArrangementFormFieldId",
                        column: x => x.ArrangementFormFieldId,
                        principalTable: "ArrangementFormFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArrangementRegistrationAnswers_ArrangementRegistrations_ArrangementRegistrationId",
                        column: x => x.ArrangementRegistrationId,
                        principalTable: "ArrangementRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArrangementRegistrationShifts",
                columns: table => new
                {
                    ArrangementRegistrationId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArrangementShiftId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArrangementRegistrationShifts", x => new { x.ArrangementRegistrationId, x.ArrangementShiftId });
                    table.ForeignKey(
                        name: "FK_ArrangementRegistrationShifts_ArrangementRegistrations_ArrangementRegistrationId",
                        column: x => x.ArrangementRegistrationId,
                        principalTable: "ArrangementRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArrangementRegistrationShifts_ArrangementShifts_ArrangementShiftId",
                        column: x => x.ArrangementShiftId,
                        principalTable: "ArrangementShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessages_ArrangementId",
                table: "CommunicationMessages",
                column: "ArrangementId");

            migrationBuilder.CreateIndex(
                name: "IX_Arrangements_PublicId",
                table: "Arrangements",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArrangementRegistrationAnswers_ArrangementFormFieldId",
                table: "ArrangementRegistrationAnswers",
                column: "ArrangementFormFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ArrangementRegistrationAnswers_ArrangementRegistrationId",
                table: "ArrangementRegistrationAnswers",
                column: "ArrangementRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_ArrangementRegistrations_ArrangementId_PersonId",
                table: "ArrangementRegistrations",
                columns: new[] { "ArrangementId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArrangementRegistrations_PersonId",
                table: "ArrangementRegistrations",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ArrangementRegistrationShifts_ArrangementShiftId",
                table: "ArrangementRegistrationShifts",
                column: "ArrangementShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationMessages_Arrangements_ArrangementId",
                table: "CommunicationMessages",
                column: "ArrangementId",
                principalTable: "Arrangements",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationMessages_Arrangements_ArrangementId",
                table: "CommunicationMessages");

            migrationBuilder.DropTable(
                name: "ArrangementRegistrationAnswers");

            migrationBuilder.DropTable(
                name: "ArrangementRegistrationShifts");

            migrationBuilder.DropTable(
                name: "ArrangementRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationMessages_ArrangementId",
                table: "CommunicationMessages");

            migrationBuilder.DropIndex(
                name: "IX_Arrangements_PublicId",
                table: "Arrangements");

            migrationBuilder.DropColumn(
                name: "ArrangementId",
                table: "CommunicationMessages");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Arrangements");
        }
    }
}
