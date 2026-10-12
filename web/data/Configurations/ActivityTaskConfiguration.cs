using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ActivityTaskConfiguration : IEntityTypeConfiguration<ActivityTask>
{
    public void Configure(EntityTypeBuilder<ActivityTask> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Note)
            .HasMaxLength(1000);

        builder.HasOne(e => e.Activity)
            .WithMany(a => a.Tasks)
            .HasForeignKey(e => e.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AssignedToWorkgroupMember)
            .WithMany(m => m.AssignedTasks)
            .HasForeignKey(e => e.AssignedToWorkgroupMemberId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.ActivityId, e.Order });
        builder.HasIndex(e => e.DeadlineAtUtc);
    }
}
