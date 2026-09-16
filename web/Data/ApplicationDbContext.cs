using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using web.Data.Entities;

namespace web.Data
{
    /// <summary>
    /// Application database context for Identity, app settings, themes and file metadata
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // App settings
        public DbSet<AppSetting> AppSettings { get; set; } = null!;

        // Theme settings
        public DbSet<ThemeSetting> ThemeSettings { get; set; } = null!;

        // File metadata (files themselves stored in App_files/)
        public DbSet<FileMetadata> FileMetadata { get; set; } = null!;

        // SMS messages (sent and received via the SMS gateway service)
        public DbSet<SmsMessage> SmsMessages { get; set; } = null!;

        // Forms: definitions (Form/FormField) and collected responses (FormSubmission/FormAnswer)
        public DbSet<Form> Forms { get; set; } = null!;
        public DbSet<FormField> FormFields { get; set; } = null!;
        public DbSet<FormSubmission> FormSubmissions { get; set; } = null!;
        public DbSet<FormAnswer> FormAnswers { get; set; } = null!;

        // People: person register with group memberships and guardian contacts
        public DbSet<Person> People { get; set; } = null!;
        public DbSet<PersonGroup> PersonGroups { get; set; } = null!;
        public DbSet<PersonGroupMembership> PersonGroupMemberships { get; set; } = null!;
        public DbSet<PersonGuardian> PersonGuardians { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ThemePreference)
                    .HasMaxLength(10);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(e => e.Email)
                    .IsUnique();
            });

            // Configure AppSetting
            builder.Entity<AppSetting>(entity =>
            {
                entity.HasKey(e => e.Key);

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Value)
                    .IsRequired();
            });

            // Configure ThemeSetting
            builder.Entity<ThemeSetting>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ThemeMode)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.HasIndex(e => new { e.Name, e.ThemeMode })
                    .IsUnique();
            });

            // Configure FileMetadata
            builder.Entity<FileMetadata>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.OriginalFileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.StoredFileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.ContentType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Category)
                    .HasMaxLength(50);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                // Optional: Relationship to ApplicationUser (owner)
                // entity.HasOne<ApplicationUser>()
                //     .WithMany()
                //     .HasForeignKey(e => e.OwnerId)
                //     .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure SmsMessage
            builder.Entity<SmsMessage>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Direction)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.PhoneNumber)
                    .IsRequired()
                    .HasMaxLength(32);

                entity.Property(e => e.Body)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.FailureReason)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(e => e.GatewayMessageId);
                entity.HasIndex(e => e.PhoneNumber);
                entity.HasIndex(e => e.CreatedAtUtc);
            });

            // Configure Form
            builder.Entity<Form>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .HasMaxLength(2000);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(e => e.CreatedAtUtc);
            });

            // Configure FormField
            builder.Entity<FormField>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Label)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.HelpText)
                    .HasMaxLength(500);

                entity.Property(e => e.FieldType)
                    .IsRequired()
                    .HasMaxLength(30)
                    .HasConversion<string>();

                entity.HasOne(e => e.Form)
                    .WithMany(f => f.Fields)
                    .HasForeignKey(e => e.FormId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.FormId, e.Order });
            });

            // Configure FormSubmission
            builder.Entity<FormSubmission>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.SubmittedByUserId)
                    .IsRequired();

                entity.Property(e => e.SubmittedAtUtc)
                    .IsRequired();

                entity.HasOne(e => e.Form)
                    .WithMany(f => f.Submissions)
                    .HasForeignKey(e => e.FormId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.FormId);
            });

            // Configure FormAnswer
            builder.Entity<FormAnswer>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.FormSubmission)
                    .WithMany(s => s.Answers)
                    .HasForeignKey(e => e.FormSubmissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.FormField)
                    .WithMany(f => f.Answers)
                    .HasForeignKey(e => e.FormFieldId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.FormSubmissionId);
                entity.HasIndex(e => e.FormFieldId);
            });

            // Configure Person
            builder.Entity<Person>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Uid)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Mobile)
                    .HasMaxLength(20);

                entity.Property(e => e.Email)
                    .HasMaxLength(256);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(e => e.Uid)
                    .IsUnique();

                entity.HasIndex(e => e.Name);
            });

            // Configure PersonGroup
            builder.Entity<PersonGroup>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(e => e.Name)
                    .IsUnique();
            });

            // Configure PersonGroupMembership (join entity for the Person <-> PersonGroup many-to-many)
            builder.Entity<PersonGroupMembership>(entity =>
            {
                entity.HasKey(e => new { e.PersonId, e.GroupId });

                entity.HasOne(e => e.Person)
                    .WithMany(p => p.Memberships)
                    .HasForeignKey(e => e.PersonId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Group)
                    .WithMany(g => g.Memberships)
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.GroupId);
            });

            // Configure PersonGuardian
            builder.Entity<PersonGuardian>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Mobile)
                    .HasMaxLength(20);

                entity.Property(e => e.Email)
                    .HasMaxLength(256);

                entity.HasOne(e => e.Person)
                    .WithMany(p => p.Guardians)
                    .HasForeignKey(e => e.PersonId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.PersonId);
            });
        }
    }
}
