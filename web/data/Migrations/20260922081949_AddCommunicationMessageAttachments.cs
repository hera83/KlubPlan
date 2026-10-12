using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationMessageAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommunicationMessageId",
                table: "CommunicationEmailMessages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommunicationMessageAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommunicationMessageId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileMetadataId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationMessageAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationMessageAttachments_CommunicationMessages_CommunicationMessageId",
                        column: x => x.CommunicationMessageId,
                        principalTable: "CommunicationMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunicationMessageAttachments_FileMetadata_FileMetadataId",
                        column: x => x.FileMetadataId,
                        principalTable: "FileMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationEmailMessages_CommunicationMessageId",
                table: "CommunicationEmailMessages",
                column: "CommunicationMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessageAttachments_CommunicationMessageId",
                table: "CommunicationMessageAttachments",
                column: "CommunicationMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessageAttachments_FileMetadataId",
                table: "CommunicationMessageAttachments",
                column: "FileMetadataId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationEmailMessages_CommunicationMessages_CommunicationMessageId",
                table: "CommunicationEmailMessages",
                column: "CommunicationMessageId",
                principalTable: "CommunicationMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationEmailMessages_CommunicationMessages_CommunicationMessageId",
                table: "CommunicationEmailMessages");

            migrationBuilder.DropTable(
                name: "CommunicationMessageAttachments");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationEmailMessages_CommunicationMessageId",
                table: "CommunicationEmailMessages");

            migrationBuilder.DropColumn(
                name: "CommunicationMessageId",
                table: "CommunicationEmailMessages");
        }
    }
}
