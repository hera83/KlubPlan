using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityWorkgroupMemberAdminLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ActivityWorkgroupMembers",
                type: "TEXT",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "ActivityWorkgroupMembers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityWorkgroupMembers_ApplicationUserId",
                table: "ActivityWorkgroupMembers",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityWorkgroupMembers_AspNetUsers_ApplicationUserId",
                table: "ActivityWorkgroupMembers",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityWorkgroupMembers_AspNetUsers_ApplicationUserId",
                table: "ActivityWorkgroupMembers");

            migrationBuilder.DropIndex(
                name: "IX_ActivityWorkgroupMembers_ApplicationUserId",
                table: "ActivityWorkgroupMembers");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "ActivityWorkgroupMembers");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ActivityWorkgroupMembers",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
