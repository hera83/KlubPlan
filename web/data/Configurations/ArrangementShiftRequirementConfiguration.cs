using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ArrangementShiftRequirementConfiguration : IEntityTypeConfiguration<ArrangementShiftRequirement>
{
    public void Configure(EntityTypeBuilder<ArrangementShiftRequirement> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Text)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(e => e.Shift)
            .WithMany(s => s.Requirements)
            .HasForeignKey(e => e.ArrangementShiftId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ArrangementShiftId, e.Order });
    }
}
