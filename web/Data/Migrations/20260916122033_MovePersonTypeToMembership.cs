using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MovePersonTypeToMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "PersonGroupMemberships",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Player");

            // Type used to live on Person (one role for all of a person's groups). Carry each
            // person's existing role over to all of their memberships before dropping it, so
            // people already marked as Træner/Ungtræner don't silently revert to Spiller.
            migrationBuilder.Sql(
                "UPDATE PersonGroupMemberships " +
                "SET Type = (SELECT Type FROM People WHERE People.Id = PersonGroupMemberships.PersonId);");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "People");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "People",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Player");

            migrationBuilder.Sql(
                "UPDATE People " +
                "SET Type = COALESCE((SELECT m.Type FROM PersonGroupMemberships m WHERE m.PersonId = People.Id LIMIT 1), 'Player');");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "PersonGroupMemberships");
        }
    }
}
