using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

/// <summary>Join entity for the Person ↔ PersonGroup many-to-many</summary>
public class PersonGroupMembershipConfiguration : IEntityTypeConfiguration<PersonGroupMembership>
{
    public void Configure(EntityTypeBuilder<PersonGroupMembership> builder)
    {
        builder.HasKey(e => new { e.PersonId, e.GroupId });

        builder.Property(e => e.Type)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.HasOne(e => e.Person)
            .WithMany(p => p.Memberships)
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Group)
            .WithMany(g => g.Memberships)
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.GroupId);
    }
}
