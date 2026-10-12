using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ThemeSettingConfiguration : IEntityTypeConfiguration<ThemeSetting>
{
    public void Configure(EntityTypeBuilder<ThemeSetting> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ThemeMode)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(e => new { e.Name, e.ThemeMode })
            .IsUnique();
    }
}
