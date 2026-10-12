using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

/// <summary>Join entity for Arrangement ↔ Person many-to-many</summary>
public class ArrangementAllowedPersonConfiguration : IEntityTypeConfiguration<ArrangementAllowedPerson>
{
    public void Configure(EntityTypeBuilder<ArrangementAllowedPerson> builder)
    {
        builder.HasKey(e => new { e.ArrangementId, e.PersonId });

        builder.HasOne(e => e.Arrangement)
            .WithMany(a => a.AllowedPersons)
            .HasForeignKey(e => e.ArrangementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.PersonId);
    }
}
