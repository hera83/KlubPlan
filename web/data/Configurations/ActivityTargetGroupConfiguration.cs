using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

/// <summary>Join entity for Activity ↔ PersonGroup many-to-many</summary>
public class ActivityTargetGroupConfiguration : IEntityTypeConfiguration<ActivityTargetGroup>
{
    public void Configure(EntityTypeBuilder<ActivityTargetGroup> builder)
    {
        builder.HasKey(e => new { e.ActivityId, e.PersonGroupId });

        builder.HasOne(e => e.Activity)
            .WithMany(a => a.TargetGroups)
            .HasForeignKey(e => e.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.PersonGroup)
            .WithMany()
            .HasForeignKey(e => e.PersonGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.PersonGroupId);
    }
}
