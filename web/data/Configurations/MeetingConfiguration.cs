using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Location)
            .HasMaxLength(200);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.MeetingDateUtc);

        builder.Property(e => e.VersionNumber)
            .IsRequired()
            .HasDefaultValue(1);

        builder.HasOne<Meeting>()
            .WithMany()
            .HasForeignKey(e => e.RootMeetingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.RootMeetingId);
    }
}
