using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInfoScreens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InfoScreens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PublicId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    AspectRatio = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Transition = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfoScreens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InfoScreenMedia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PublicId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InfoScreenId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileMetadataId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfoScreenMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InfoScreenMedia_FileMetadata_FileMetadataId",
                        column: x => x.FileMetadataId,
                        principalTable: "FileMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InfoScreenMedia_InfoScreens_InfoScreenId",
                        column: x => x.InfoScreenId,
                        principalTable: "InfoScreens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InfoScreenSlides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InfoScreenId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Background = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsHidden = table.Column<bool>(type: "INTEGER", nullable: false),
                    ElementsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfoScreenSlides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InfoScreenSlides_InfoScreens_InfoScreenId",
                        column: x => x.InfoScreenId,
                        principalTable: "InfoScreens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InfoScreenMedia_FileMetadataId",
                table: "InfoScreenMedia",
                column: "FileMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_InfoScreenMedia_InfoScreenId",
                table: "InfoScreenMedia",
                column: "InfoScreenId");

            migrationBuilder.CreateIndex(
                name: "IX_InfoScreenMedia_PublicId",
                table: "InfoScreenMedia",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InfoScreens_PublicId",
                table: "InfoScreens",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InfoScreens_Title",
                table: "InfoScreens",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_InfoScreenSlides_InfoScreenId_Order",
                table: "InfoScreenSlides",
                columns: new[] { "InfoScreenId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InfoScreenMedia");

            migrationBuilder.DropTable(
                name: "InfoScreenSlides");

            migrationBuilder.DropTable(
                name: "InfoScreens");
        }
    }
}
