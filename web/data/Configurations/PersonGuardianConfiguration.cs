using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class PersonGuardianConfiguration : IEntityTypeConfiguration<PersonGuardian>
{
    public void Configure(EntityTypeBuilder<PersonGuardian> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Mobile)
            .HasMaxLength(20);

        builder.Property(e => e.Email)
            .HasMaxLength(256);

        builder.HasOne(e => e.Person)
            .WithMany(p => p.Guardians)
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.PersonId);
    }
}
