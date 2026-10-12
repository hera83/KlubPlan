using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ArrangementRegistrationConfiguration : IEntityTypeConfiguration<ArrangementRegistration>
{
    public void Configure(EntityTypeBuilder<ArrangementRegistration> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RegisteredAtUtc)
            .IsRequired();

        builder.HasOne(e => e.Arrangement)
            .WithMany(a => a.Registrations)
            .HasForeignKey(e => e.ArrangementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        // One registration per person per arrangement — enforces "can't sign up twice" at
        // the database level too, not just via the app-level gate check.
        builder.HasIndex(e => new { e.ArrangementId, e.PersonId })
            .IsUnique();
    }
}
