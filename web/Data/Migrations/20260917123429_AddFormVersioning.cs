using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFormVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RootFormId",
                table: "Forms",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "Forms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Forms_RootFormId",
                table: "Forms",
                column: "RootFormId");

            migrationBuilder.AddForeignKey(
                name: "FK_Forms_Forms_RootFormId",
                table: "Forms",
                column: "RootFormId",
                principalTable: "Forms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Forms_Forms_RootFormId",
                table: "Forms");

            migrationBuilder.DropIndex(
                name: "IX_Forms_RootFormId",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "RootFormId",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "Forms");
        }
    }
}
