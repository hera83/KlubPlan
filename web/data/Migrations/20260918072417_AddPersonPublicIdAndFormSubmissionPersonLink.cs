using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonPublicIdAndFormSubmissionPersonLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "People",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "SubmittedByPersonId",
                table: "FormSubmissions",
                type: "INTEGER",
                nullable: true);

            // Every existing row got the same all-zero default above — give each a distinct,
            // random id before the unique index below is created.
            migrationBuilder.Sql(@"
                UPDATE People
                SET PublicId =
                    lower(hex(randomblob(4))) || '-' ||
                    lower(hex(randomblob(2))) || '-' ||
                    lower(hex(randomblob(2))) || '-' ||
                    lower(hex(randomblob(2))) || '-' ||
                    lower(hex(randomblob(6)));
            ");

            migrationBuilder.CreateIndex(
                name: "IX_People_PublicId",
                table: "People",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_People_PublicId",
                table: "People");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "People");

            migrationBuilder.DropColumn(
                name: "SubmittedByPersonId",
                table: "FormSubmissions");
        }
    }
}
