using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ActivityListItemConfiguration : IEntityTypeConfiguration<ActivityListItem>
{
    public void Configure(EntityTypeBuilder<ActivityListItem> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Note)
            .HasMaxLength(2000);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.ActivityList)
            .WithMany(l => l.Items)
            .HasForeignKey(e => e.ActivityListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Status)
            .WithMany()
            .HasForeignKey(e => e.StatusId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.AssignedToWorkgroupMember)
            .WithMany()
            .HasForeignKey(e => e.AssignedToWorkgroupMemberId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.UpdatedByUser)
            .WithMany()
            .HasForeignKey(e => e.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.UpdatedByWorkgroupMember)
            .WithMany()
            .HasForeignKey(e => e.UpdatedByWorkgroupMemberId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.ActivityListId, e.Order });
        builder.HasIndex(e => e.AssignedToWorkgroupMemberId);
    }
}
