using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ActivityListColumnConfiguration : IEntityTypeConfiguration<ActivityListColumn>
{
    public void Configure(EntityTypeBuilder<ActivityListColumn> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Kind)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(e => e.Options)
            .HasMaxLength(2000);

        builder.HasOne(e => e.ActivityList)
            .WithMany(l => l.Columns)
            .HasForeignKey(e => e.ActivityListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ActivityListId, e.Order });
    }
}
