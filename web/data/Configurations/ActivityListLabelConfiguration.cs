using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ActivityListLabelConfiguration : IEntityTypeConfiguration<ActivityListLabel>
{
    public void Configure(EntityTypeBuilder<ActivityListLabel> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Text)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.Item)
            .WithMany(i => i.Labels)
            .HasForeignKey(e => e.ActivityListItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ActivityListItemId, e.Order });
    }
}
