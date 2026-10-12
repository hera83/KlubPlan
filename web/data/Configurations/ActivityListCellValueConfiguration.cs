using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ActivityListCellValueConfiguration : IEntityTypeConfiguration<ActivityListCellValue>
{
    public void Configure(EntityTypeBuilder<ActivityListCellValue> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Value)
            .HasMaxLength(4000);

        builder.HasOne(e => e.Item)
            .WithMany(i => i.Values)
            .HasForeignKey(e => e.ActivityListItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Column)
            .WithMany(c => c.Values)
            .HasForeignKey(e => e.ActivityListColumnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ActivityListItemId, e.ActivityListColumnId }).IsUnique();
        builder.HasIndex(e => e.ActivityListColumnId);
    }
}
