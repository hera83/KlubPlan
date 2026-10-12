using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class MeetingDecisionConfiguration : IEntityTypeConfiguration<MeetingDecision>
{
    public void Configure(EntityTypeBuilder<MeetingDecision> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.Meeting)
            .WithMany(m => m.Decisions)
            .HasForeignKey(e => e.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ResponsibleUser)
            .WithMany()
            .HasForeignKey(e => e.ResponsibleUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.MeetingId);
    }
}
