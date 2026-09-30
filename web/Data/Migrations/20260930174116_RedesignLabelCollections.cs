using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RedesignLabelCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The old text-only label collections can't be carried over to designed labels — they are removed
            // (decided when the label designer replaced the text rows).
            migrationBuilder.Sql("DELETE FROM LabelCollectionItems;");
            migrationBuilder.Sql("DELETE FROM LabelCollections;");

            migrationBuilder.DropColumn(
                name: "Text",
                table: "LabelCollectionItems");

            migrationBuilder.AddColumn<int>(
                name: "Across",
                table: "LabelCollections",
                type: "INTEGER",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "Down",
                table: "LabelCollections",
                type: "INTEGER",
                nullable: false,
                defaultValue: 8);

            migrationBuilder.AddColumn<bool>(
                name: "Landscape",
                table: "LabelCollections",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Background",
                table: "LabelCollectionItems",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "#ffffff");

            migrationBuilder.AddColumn<string>(
                name: "ElementsJson",
                table: "LabelCollectionItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "LabelCollectionMedia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PublicId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LabelCollectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileMetadataId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelCollectionMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabelCollectionMedia_FileMetadata_FileMetadataId",
                        column: x => x.FileMetadataId,
                        principalTable: "FileMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabelCollectionMedia_LabelCollections_LabelCollectionId",
                        column: x => x.LabelCollectionId,
                        principalTable: "LabelCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabelCollectionMedia_FileMetadataId",
                table: "LabelCollectionMedia",
                column: "FileMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_LabelCollectionMedia_LabelCollectionId",
                table: "LabelCollectionMedia",
                column: "LabelCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_LabelCollectionMedia_PublicId",
                table: "LabelCollectionMedia",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabelCollectionMedia");

            migrationBuilder.DropColumn(
                name: "Across",
                table: "LabelCollections");

            migrationBuilder.DropColumn(
                name: "Down",
                table: "LabelCollections");

            migrationBuilder.DropColumn(
                name: "Landscape",
                table: "LabelCollections");

            migrationBuilder.DropColumn(
                name: "Background",
                table: "LabelCollectionItems");

            migrationBuilder.DropColumn(
                name: "ElementsJson",
                table: "LabelCollectionItems");

            migrationBuilder.AddColumn<string>(
                name: "Text",
                table: "LabelCollectionItems",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
