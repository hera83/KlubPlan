using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ArrangementShiftConfiguration : IEntityTypeConfiguration<ArrangementShift>
{
    public void Configure(EntityTypeBuilder<ArrangementShift> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Location)
            .HasMaxLength(200);

        builder.HasOne(e => e.Arrangement)
            .WithMany(a => a.Shifts)
            .HasForeignKey(e => e.ArrangementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ArrangementId, e.Order });
        builder.HasIndex(e => e.StartUtc);
    }
}
