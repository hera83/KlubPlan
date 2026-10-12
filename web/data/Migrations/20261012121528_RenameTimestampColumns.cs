using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.data.Migrations
{
    /// <inheritdoc />
    public partial class RenameTimestampColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "ThemeSettings",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ThemeSettings",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "SmsMessages",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "SmsMessages",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_SmsMessages_CreatedAtUtc",
                table: "SmsMessages",
                newName: "IX_SmsMessages_CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "PersonGroups",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "PersonGroups",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "People",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "People",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "Meetings",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Meetings",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "MeetingDecisions",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "MeetingAttachments",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "LabelCollections",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "LabelCollections",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "LabelCollectionMedia",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "LabelCollectionItems",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "LabelCollectionItems",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "InfoScreenSlides",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "InfoScreenSlides",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "InfoScreens",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "InfoScreens",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "InfoScreenMedia",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "Forms",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Forms",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Forms_CreatedAtUtc",
                table: "Forms",
                newName: "IX_Forms_CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "FileMetadata",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "FileMetadata",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "CommunicationMessages",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "CommunicationMessages",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_CommunicationMessages_CreatedAtUtc",
                table: "CommunicationMessages",
                newName: "IX_CommunicationMessages_CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "CommunicationMessageRecipients",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "CommunicationMessageAttachments",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "CommunicationEmailMessages",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "CommunicationEmailMessages",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_CommunicationEmailMessages_CreatedAtUtc",
                table: "CommunicationEmailMessages",
                newName: "IX_CommunicationEmailMessages_CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "Arrangements",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Arrangements",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Arrangements_CreatedAtUtc",
                table: "Arrangements",
                newName: "IX_Arrangements_CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ActivityWorkgroupMembers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ActivityTasks",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "ActivityLists",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ActivityLists",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "ActivityListLabels",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ActivityListLabels",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "ActivityListItems",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ActivityListItems",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ActivityListColumns",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "ActivityFolders",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ActivityFolders",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ActivityFileVersions",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "ActivityFiles",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ActivityFiles",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "Activities",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Activities",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Activities_CreatedAtUtc",
                table: "Activities",
                newName: "IX_Activities_CreatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PersonGuardians",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PersonGuardians",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MeetingDecisions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MeetingAttachments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "LabelCollectionMedia",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "InfoScreenMedia",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FormSubmissions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FormSubmissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FormFields",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FormFields",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FormAnswers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FormAnswers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "CommunicationMessageRecipients",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "CommunicationMessageAttachments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ArrangementShifts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ArrangementShifts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ArrangementShiftRequirements",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ArrangementShiftRequirements",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ArrangementRegistrationShifts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ArrangementRegistrationShifts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ArrangementRegistrations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ArrangementRegistrations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ArrangementRegistrationAnswers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ArrangementRegistrationAnswers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ArrangementFormFields",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ArrangementFormFields",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ActivityWorkgroupMembers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ActivityTasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ActivityListStatuses",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ActivityListStatuses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ActivityListColumns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ActivityListCellValues",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ActivityListCellValues",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ActivityFileVersions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PersonGuardians");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PersonGuardians");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MeetingDecisions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MeetingAttachments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "LabelCollectionMedia");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "InfoScreenMedia");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FormFields");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FormFields");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FormAnswers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FormAnswers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CommunicationMessageRecipients");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CommunicationMessageAttachments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ArrangementShifts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ArrangementShifts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ArrangementShiftRequirements");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ArrangementShiftRequirements");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ArrangementRegistrationShifts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ArrangementRegistrationShifts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ArrangementRegistrations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ArrangementRegistrations");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ArrangementRegistrationAnswers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ArrangementRegistrationAnswers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ArrangementFormFields");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ArrangementFormFields");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ActivityWorkgroupMembers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ActivityTasks");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ActivityListStatuses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ActivityListStatuses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ActivityListColumns");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ActivityListCellValues");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ActivityListCellValues");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ActivityFileVersions");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ThemeSettings",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ThemeSettings",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "SmsMessages",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "SmsMessages",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_SmsMessages_CreatedAt",
                table: "SmsMessages",
                newName: "IX_SmsMessages_CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "PersonGroups",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "PersonGroups",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "People",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "People",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Meetings",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Meetings",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "MeetingDecisions",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "MeetingAttachments",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "LabelCollections",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "LabelCollections",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "LabelCollectionMedia",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "LabelCollectionItems",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "LabelCollectionItems",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "InfoScreenSlides",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "InfoScreenSlides",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "InfoScreens",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "InfoScreens",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "InfoScreenMedia",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Forms",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Forms",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_Forms_CreatedAt",
                table: "Forms",
                newName: "IX_Forms_CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "FileMetadata",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "FileMetadata",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "CommunicationMessages",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "CommunicationMessages",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_CommunicationMessages_CreatedAt",
                table: "CommunicationMessages",
                newName: "IX_CommunicationMessages_CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "CommunicationMessageRecipients",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "CommunicationMessageAttachments",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "CommunicationEmailMessages",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "CommunicationEmailMessages",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_CommunicationEmailMessages_CreatedAt",
                table: "CommunicationEmailMessages",
                newName: "IX_CommunicationEmailMessages_CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Arrangements",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Arrangements",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_Arrangements_CreatedAt",
                table: "Arrangements",
                newName: "IX_Arrangements_CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ActivityWorkgroupMembers",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ActivityTasks",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ActivityLists",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ActivityLists",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ActivityListLabels",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ActivityListLabels",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ActivityListItems",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ActivityListItems",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ActivityListColumns",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ActivityFolders",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ActivityFolders",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ActivityFileVersions",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ActivityFiles",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ActivityFiles",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Activities",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Activities",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_Activities_CreatedAt",
                table: "Activities",
                newName: "IX_Activities_CreatedAtUtc");
        }
    }
}
