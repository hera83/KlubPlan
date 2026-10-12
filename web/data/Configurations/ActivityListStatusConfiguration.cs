using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ActivityListStatusConfiguration : IEntityTypeConfiguration<ActivityListStatus>
{
    public void Configure(EntityTypeBuilder<ActivityListStatus> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Color)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasOne(e => e.ActivityList)
            .WithMany(l => l.Statuses)
            .HasForeignKey(e => e.ActivityListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ActivityListId, e.Order });
    }
}
