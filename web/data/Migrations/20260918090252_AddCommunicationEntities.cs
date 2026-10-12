using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunicationEmailMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ToAddress = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationEmailMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    ViaEmail = table.Column<bool>(type: "INTEGER", nullable: false),
                    ViaSms = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FormId = table.Column<int>(type: "INTEGER", nullable: true),
                    LinkType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    RecipientSummary = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    RecipientCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationMessages_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationMessageGroups",
                columns: table => new
                {
                    CommunicationMessageId = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonGroupId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationMessageGroups", x => new { x.CommunicationMessageId, x.PersonGroupId });
                    table.ForeignKey(
                        name: "FK_CommunicationMessageGroups_CommunicationMessages_CommunicationMessageId",
                        column: x => x.CommunicationMessageId,
                        principalTable: "CommunicationMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunicationMessageGroups_PersonGroups_PersonGroupId",
                        column: x => x.PersonGroupId,
                        principalTable: "PersonGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationMessageRecipientPersons",
                columns: table => new
                {
                    CommunicationMessageId = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationMessageRecipientPersons", x => new { x.CommunicationMessageId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_CommunicationMessageRecipientPersons_CommunicationMessages_CommunicationMessageId",
                        column: x => x.CommunicationMessageId,
                        principalTable: "CommunicationMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunicationMessageRecipientPersons_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationMessageRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommunicationMessageId = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonId = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Channel = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    SmsMessageId = table.Column<int>(type: "INTEGER", nullable: true),
                    CommunicationEmailMessageId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationMessageRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationMessageRecipients_CommunicationEmailMessages_CommunicationEmailMessageId",
                        column: x => x.CommunicationEmailMessageId,
                        principalTable: "CommunicationEmailMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CommunicationMessageRecipients_CommunicationMessages_CommunicationMessageId",
                        column: x => x.CommunicationMessageId,
                        principalTable: "CommunicationMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunicationMessageRecipients_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunicationMessageRecipients_SmsMessages_SmsMessageId",
                        column: x => x.SmsMessageId,
                        principalTable: "SmsMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationEmailMessages_CreatedAtUtc",
                table: "CommunicationEmailMessages",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationEmailMessages_ToAddress",
                table: "CommunicationEmailMessages",
                column: "ToAddress");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessageGroups_PersonGroupId",
                table: "CommunicationMessageGroups",
                column: "PersonGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessageRecipientPersons_PersonId",
                table: "CommunicationMessageRecipientPersons",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessageRecipients_Channel_Address",
                table: "CommunicationMessageRecipients",
                columns: new[] { "Channel", "Address" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessageRecipients_CommunicationEmailMessageId",
                table: "CommunicationMessageRecipients",
                column: "CommunicationEmailMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessageRecipients_CommunicationMessageId",
                table: "CommunicationMessageRecipients",
                column: "CommunicationMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessageRecipients_PersonId",
                table: "CommunicationMessageRecipients",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessageRecipients_SmsMessageId",
                table: "CommunicationMessageRecipients",
                column: "SmsMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessages_CreatedAtUtc",
                table: "CommunicationMessages",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessages_FormId",
                table: "CommunicationMessages",
                column: "FormId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunicationMessageGroups");

            migrationBuilder.DropTable(
                name: "CommunicationMessageRecipientPersons");

            migrationBuilder.DropTable(
                name: "CommunicationMessageRecipients");

            migrationBuilder.DropTable(
                name: "CommunicationEmailMessages");

            migrationBuilder.DropTable(
                name: "CommunicationMessages");
        }
    }
}
