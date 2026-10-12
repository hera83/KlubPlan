using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingAttachmentTranscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TranscriptionCompletedAtUtc",
                table: "MeetingAttachments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranscriptionError",
                table: "MeetingAttachments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TranscriptionStartedAtUtc",
                table: "MeetingAttachments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranscriptionStatus",
                table: "MeetingAttachments",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TranscriptionCompletedAtUtc",
                table: "MeetingAttachments");

            migrationBuilder.DropColumn(
                name: "TranscriptionError",
                table: "MeetingAttachments");

            migrationBuilder.DropColumn(
                name: "TranscriptionStartedAtUtc",
                table: "MeetingAttachments");

            migrationBuilder.DropColumn(
                name: "TranscriptionStatus",
                table: "MeetingAttachments");
        }
    }
}
