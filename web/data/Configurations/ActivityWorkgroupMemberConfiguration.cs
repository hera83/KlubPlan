using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

/// <summary>Either a linked administrator or an external contact</summary>
public class ActivityWorkgroupMemberConfiguration : IEntityTypeConfiguration<ActivityWorkgroupMember>
{
    public void Configure(EntityTypeBuilder<ActivityWorkgroupMember> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PublicId)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.Name)
            .HasMaxLength(200);

        builder.Property(e => e.Role)
            .HasMaxLength(100);

        builder.Property(e => e.Email)
            .HasMaxLength(200);

        builder.Property(e => e.Mobile)
            .HasMaxLength(30);

        builder.HasOne(e => e.Activity)
            .WithMany(a => a.WorkgroupMembers)
            .HasForeignKey(e => e.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ApplicationUser)
            .WithMany()
            .HasForeignKey(e => e.ApplicationUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.ActivityId, e.Order });
        builder.HasIndex(e => e.ApplicationUserId);
        builder.HasIndex(e => e.PublicId)
            .IsUnique();
    }
}
