using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkgroupMemberPublicLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "ActivityWorkgroupMembers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByWorkgroupMemberId",
                table: "ActivityListItems",
                type: "INTEGER",
                nullable: true);

            // Every existing row got the same empty default above — give each a distinct,
            // random id before the unique index below is created.
            migrationBuilder.Sql(@"
                UPDATE ActivityWorkgroupMembers
                SET PublicId =
                    lower(hex(randomblob(4))) || '-' ||
                    lower(hex(randomblob(2))) || '-' ||
                    lower(hex(randomblob(2))) || '-' ||
                    lower(hex(randomblob(2))) || '-' ||
                    lower(hex(randomblob(6)));
            ");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityWorkgroupMembers_PublicId",
                table: "ActivityWorkgroupMembers",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityListItems_UpdatedByWorkgroupMemberId",
                table: "ActivityListItems",
                column: "UpdatedByWorkgroupMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityListItems_ActivityWorkgroupMembers_UpdatedByWorkgroupMemberId",
                table: "ActivityListItems",
                column: "UpdatedByWorkgroupMemberId",
                principalTable: "ActivityWorkgroupMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityListItems_ActivityWorkgroupMembers_UpdatedByWorkgroupMemberId",
                table: "ActivityListItems");

            migrationBuilder.DropIndex(
                name: "IX_ActivityWorkgroupMembers_PublicId",
                table: "ActivityWorkgroupMembers");

            migrationBuilder.DropIndex(
                name: "IX_ActivityListItems_UpdatedByWorkgroupMemberId",
                table: "ActivityListItems");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "ActivityWorkgroupMembers");

            migrationBuilder.DropColumn(
                name: "UpdatedByWorkgroupMemberId",
                table: "ActivityListItems");
        }
    }
}
