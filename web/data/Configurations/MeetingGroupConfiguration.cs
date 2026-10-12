using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

/// <summary>Join entity for the Meeting ↔ PersonGroup many-to-many</summary>
public class MeetingGroupConfiguration : IEntityTypeConfiguration<MeetingGroup>
{
    public void Configure(EntityTypeBuilder<MeetingGroup> builder)
    {
        builder.HasKey(e => new { e.MeetingId, e.PersonGroupId });

        builder.HasOne(e => e.Meeting)
            .WithMany(m => m.Groups)
            .HasForeignKey(e => e.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.PersonGroup)
            .WithMany()
            .HasForeignKey(e => e.PersonGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.PersonGroupId);
    }
}
