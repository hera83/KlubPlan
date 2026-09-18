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

        // Meetings: administrator meetings with agenda/minutes, attendance, decisions and attachments
        public DbSet<Meeting> Meetings { get; set; } = null!;
        public DbSet<MeetingGroup> MeetingGroups { get; set; } = null!;
        public DbSet<MeetingAttendee> MeetingAttendees { get; set; } = null!;
        public DbSet<MeetingDecision> MeetingDecisions { get; set; } = null!;
        public DbSet<MeetingAttachment> MeetingAttachments { get; set; } = null!;

        // Arrangementer (Tilmeldinger): arrangement with its own signup-form fields, shifts with
        // per-shift requirements, registration window and access list (Person/PersonGroup)
        public DbSet<Arrangement> Arrangements { get; set; } = null!;
        public DbSet<ArrangementFormField> ArrangementFormFields { get; set; } = null!;
        public DbSet<ArrangementShift> ArrangementShifts { get; set; } = null!;
        public DbSet<ArrangementShiftRequirement> ArrangementShiftRequirements { get; set; } = null!;
        public DbSet<ArrangementAllowedPerson> ArrangementAllowedPersons { get; set; } = null!;
        public DbSet<ArrangementAllowedGroup> ArrangementAllowedGroups { get; set; } = null!;

        // Kommunikation: broadcast messages to Persons/PersonGroups over Email/SMS, with resolved
        // per-recipient audit trail (CommunicationMessageRecipient) and the email outbound queue
        public DbSet<CommunicationMessage> CommunicationMessages { get; set; } = null!;
        public DbSet<CommunicationMessageGroup> CommunicationMessageGroups { get; set; } = null!;
        public DbSet<CommunicationMessageRecipientPerson> CommunicationMessageRecipientPersons { get; set; } = null!;
        public DbSet<CommunicationMessageRecipient> CommunicationMessageRecipients { get; set; } = null!;
        public DbSet<CommunicationEmailMessage> CommunicationEmailMessages { get; set; } = null!;

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

                entity.Property(e => e.PublicId)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .HasMaxLength(2000);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.Property(e => e.VersionNumber)
                    .IsRequired()
                    .HasDefaultValue(1);

                entity.HasOne<Form>()
                    .WithMany()
                    .HasForeignKey(e => e.RootFormId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.CreatedAtUtc);
                entity.HasIndex(e => e.RootFormId);
                entity.HasIndex(e => e.PublicId)
                    .IsUnique();
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

                entity.Property(e => e.PublicId)
                    .IsRequired()
                    .HasConversion<string>();

                entity.HasIndex(e => e.Uid)
                    .IsUnique();

                entity.HasIndex(e => e.PublicId)
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

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasConversion<string>();

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

            // Configure Meeting
            builder.Entity<Meeting>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Location)
                    .HasMaxLength(200);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasConversion<string>();

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(e => e.MeetingDateUtc);

                entity.Property(e => e.VersionNumber)
                    .IsRequired()
                    .HasDefaultValue(1);

                entity.HasOne<Meeting>()
                    .WithMany()
                    .HasForeignKey(e => e.RootMeetingId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.RootMeetingId);
            });

            // Configure MeetingGroup (join entity for the Meeting <-> PersonGroup many-to-many)
            builder.Entity<MeetingGroup>(entity =>
            {
                entity.HasKey(e => new { e.MeetingId, e.PersonGroupId });

                entity.HasOne(e => e.Meeting)
                    .WithMany(m => m.Groups)
                    .HasForeignKey(e => e.MeetingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.PersonGroup)
                    .WithMany()
                    .HasForeignKey(e => e.PersonGroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.PersonGroupId);
            });

            // Configure MeetingAttendee (join entity for the Meeting <-> ApplicationUser many-to-many)
            builder.Entity<MeetingAttendee>(entity =>
            {
                entity.HasKey(e => new { e.MeetingId, e.ApplicationUserId });

                entity.HasOne(e => e.Meeting)
                    .WithMany(m => m.Attendees)
                    .HasForeignKey(e => e.MeetingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ApplicationUser)
                    .WithMany()
                    .HasForeignKey(e => e.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ApplicationUserId);
            });

            // Configure MeetingDecision
            builder.Entity<MeetingDecision>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasOne(e => e.Meeting)
                    .WithMany(m => m.Decisions)
                    .HasForeignKey(e => e.MeetingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ResponsibleUser)
                    .WithMany()
                    .HasForeignKey(e => e.ResponsibleUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.MeetingId);
            });

            // Configure MeetingAttachment
            builder.Entity<MeetingAttachment>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.Property(e => e.TranscriptionStatus)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasConversion<string>();

                entity.HasOne(e => e.Meeting)
                    .WithMany(m => m.Attachments)
                    .HasForeignKey(e => e.MeetingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.FileMetadata)
                    .WithMany()
                    .HasForeignKey(e => e.FileMetadataId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.MeetingId);
            });

            // Configure Arrangement
            builder.Entity<Arrangement>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .HasMaxLength(2000);

                entity.Property(e => e.AccessMode)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasConversion<string>();

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(e => e.CreatedAtUtc);
            });

            // Configure ArrangementFormField
            builder.Entity<ArrangementFormField>(entity =>
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

                entity.HasOne(e => e.Arrangement)
                    .WithMany(a => a.FormFields)
                    .HasForeignKey(e => e.ArrangementId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.ArrangementId, e.Order });
            });

            // Configure ArrangementShift
            builder.Entity<ArrangementShift>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Location)
                    .HasMaxLength(200);

                entity.HasOne(e => e.Arrangement)
                    .WithMany(a => a.Shifts)
                    .HasForeignKey(e => e.ArrangementId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.ArrangementId, e.Order });
                entity.HasIndex(e => e.StartUtc);
            });

            // Configure ArrangementShiftRequirement
            builder.Entity<ArrangementShiftRequirement>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Text)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.HasOne(e => e.Shift)
                    .WithMany(s => s.Requirements)
                    .HasForeignKey(e => e.ArrangementShiftId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.ArrangementShiftId, e.Order });
            });

            // Configure ArrangementAllowedPerson (join entity for Arrangement <-> Person many-to-many)
            builder.Entity<ArrangementAllowedPerson>(entity =>
            {
                entity.HasKey(e => new { e.ArrangementId, e.PersonId });

                entity.HasOne(e => e.Arrangement)
                    .WithMany(a => a.AllowedPersons)
                    .HasForeignKey(e => e.ArrangementId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Person)
                    .WithMany()
                    .HasForeignKey(e => e.PersonId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.PersonId);
            });

            // Configure ArrangementAllowedGroup (join entity for Arrangement <-> PersonGroup many-to-many)
            builder.Entity<ArrangementAllowedGroup>(entity =>
            {
                entity.HasKey(e => new { e.ArrangementId, e.PersonGroupId });

                entity.HasOne(e => e.Arrangement)
                    .WithMany(a => a.AllowedGroups)
                    .HasForeignKey(e => e.ArrangementId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.PersonGroup)
                    .WithMany()
                    .HasForeignKey(e => e.PersonGroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.PersonGroupId);
            });

            // Configure CommunicationMessage
            builder.Entity<CommunicationMessage>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Subject)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Body)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasConversion<string>();

                entity.Property(e => e.LinkType)
                    .HasMaxLength(20);

                entity.Property(e => e.RecipientSummary)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasOne(e => e.Form)
                    .WithMany()
                    .HasForeignKey(e => e.FormId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.CreatedAtUtc);
            });

            // Configure CommunicationMessageGroup (join entity for CommunicationMessage <-> PersonGroup many-to-many)
            builder.Entity<CommunicationMessageGroup>(entity =>
            {
                entity.HasKey(e => new { e.CommunicationMessageId, e.PersonGroupId });

                entity.HasOne(e => e.CommunicationMessage)
                    .WithMany(m => m.Groups)
                    .HasForeignKey(e => e.CommunicationMessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.PersonGroup)
                    .WithMany()
                    .HasForeignKey(e => e.PersonGroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.PersonGroupId);
            });

            // Configure CommunicationMessageRecipientPerson (join entity for CommunicationMessage <-> Person many-to-many)
            builder.Entity<CommunicationMessageRecipientPerson>(entity =>
            {
                entity.HasKey(e => new { e.CommunicationMessageId, e.PersonId });

                entity.HasOne(e => e.CommunicationMessage)
                    .WithMany(m => m.DirectPersons)
                    .HasForeignKey(e => e.CommunicationMessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Person)
                    .WithMany()
                    .HasForeignKey(e => e.PersonId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.PersonId);
            });

            // Configure CommunicationMessageRecipient (resolved, deduped send targets)
            builder.Entity<CommunicationMessageRecipient>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Channel)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.Address)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasOne(e => e.CommunicationMessage)
                    .WithMany(m => m.Recipients)
                    .HasForeignKey(e => e.CommunicationMessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Person)
                    .WithMany()
                    .HasForeignKey(e => e.PersonId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SmsMessage)
                    .WithMany()
                    .HasForeignKey(e => e.SmsMessageId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.CommunicationEmailMessage)
                    .WithMany()
                    .HasForeignKey(e => e.CommunicationEmailMessageId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.CommunicationMessageId);
                entity.HasIndex(e => new { e.Channel, e.Address });
            });

            // Configure CommunicationEmailMessage (outbound email queue, mirrors SmsMessage)
            builder.Entity<CommunicationEmailMessage>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ToAddress)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.Subject)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Body)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasConversion<string>();

                entity.Property(e => e.FailureReason)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(e => e.CreatedAtUtc);
                entity.HasIndex(e => e.ToAddress);
            });
        }
    }
}
