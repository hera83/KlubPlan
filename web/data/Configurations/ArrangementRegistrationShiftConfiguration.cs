using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

/// <summary>Join entity: which shifts a registration covers — possibly several rows per shift when the arrangement allows named companions</summary>
public class ArrangementRegistrationShiftConfiguration : IEntityTypeConfiguration<ArrangementRegistrationShift>
{
    public void Configure(EntityTypeBuilder<ArrangementRegistrationShift> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CompanionName)
            .HasMaxLength(200);

        builder.HasOne(e => e.ArrangementRegistration)
            .WithMany(r => r.Shifts)
            .HasForeignKey(e => e.ArrangementRegistrationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ArrangementShift)
            .WithMany(s => s.Registrations)
            .HasForeignKey(e => e.ArrangementShiftId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ArrangementShiftId);
    }
}
