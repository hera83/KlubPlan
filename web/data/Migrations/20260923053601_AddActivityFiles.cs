using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentFolderId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityFolders_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityFolders_ActivityFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "ActivityFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<int>(type: "INTEGER", nullable: false),
                    FolderId = table.Column<int>(type: "INTEGER", nullable: true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityFiles_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityFiles_ActivityFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "ActivityFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityFileVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityFileId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileMetadataId = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityFileVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityFileVersions_ActivityFiles_ActivityFileId",
                        column: x => x.ActivityFileId,
                        principalTable: "ActivityFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityFileVersions_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActivityFileVersions_FileMetadata_FileMetadataId",
                        column: x => x.FileMetadataId,
                        principalTable: "FileMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityFiles_ActivityId_FolderId",
                table: "ActivityFiles",
                columns: new[] { "ActivityId", "FolderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityFiles_FolderId",
                table: "ActivityFiles",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityFileVersions_ActivityFileId_VersionNumber",
                table: "ActivityFileVersions",
                columns: new[] { "ActivityFileId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityFileVersions_FileMetadataId",
                table: "ActivityFileVersions",
                column: "FileMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityFileVersions_UploadedByUserId",
                table: "ActivityFileVersions",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityFolders_ActivityId_ParentFolderId",
                table: "ActivityFolders",
                columns: new[] { "ActivityId", "ParentFolderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityFolders_ParentFolderId",
                table: "ActivityFolders",
                column: "ParentFolderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityFileVersions");

            migrationBuilder.DropTable(
                name: "ActivityFiles");

            migrationBuilder.DropTable(
                name: "ActivityFolders");
        }
    }
}
