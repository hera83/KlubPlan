using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

/// <summary>Join entity for the Meeting ↔ ApplicationUser many-to-many</summary>
public class MeetingAttendeeConfiguration : IEntityTypeConfiguration<MeetingAttendee>
{
    public void Configure(EntityTypeBuilder<MeetingAttendee> builder)
    {
        builder.HasKey(e => new { e.MeetingId, e.ApplicationUserId });

        builder.HasOne(e => e.Meeting)
            .WithMany(m => m.Attendees)
            .HasForeignKey(e => e.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ApplicationUser)
            .WithMany()
            .HasForeignKey(e => e.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ApplicationUserId);
    }
}
