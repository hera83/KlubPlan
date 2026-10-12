using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using web.Data.Models;

namespace web.Data;

/// <summary>
/// Application database context (app.db) for Identity and all of the app's own tables.
/// Table configuration lives in data/Configurations/ (one IEntityTypeConfiguration per table).
/// </summary>
public class WebDbContext(DbContextOptions<WebDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    // App settings
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    // Theme settings
    public DbSet<ThemeSetting> ThemeSettings => Set<ThemeSetting>();

    // File metadata (files themselves stored in app_files/)
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();

    // SMS messages (sent and received via the SMS gateway service)
    public DbSet<SmsMessage> SmsMessages => Set<SmsMessage>();

    // Forms: definitions (Form/FormField) and collected responses (FormSubmission/FormAnswer)
    public DbSet<Form> Forms => Set<Form>();
    public DbSet<FormField> FormFields => Set<FormField>();
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();
    public DbSet<FormAnswer> FormAnswers => Set<FormAnswer>();

    // People: person register with group memberships and guardian contacts
    public DbSet<Person> People => Set<Person>();
    public DbSet<PersonGroup> PersonGroups => Set<PersonGroup>();
    public DbSet<PersonGroupMembership> PersonGroupMemberships => Set<PersonGroupMembership>();
    public DbSet<PersonGuardian> PersonGuardians => Set<PersonGuardian>();

    // Meetings: administrator meetings with agenda/minutes, attendance, decisions and attachments
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingGroup> MeetingGroups => Set<MeetingGroup>();
    public DbSet<MeetingAttendee> MeetingAttendees => Set<MeetingAttendee>();
    public DbSet<MeetingDecision> MeetingDecisions => Set<MeetingDecision>();
    public DbSet<MeetingAttachment> MeetingAttachments => Set<MeetingAttachment>();

    // Arrangementer (Tilmeldinger): arrangement with its own signup-form fields, shifts with
    // per-shift requirements, registration window and access list (Person/PersonGroup)
    public DbSet<Arrangement> Arrangements => Set<Arrangement>();
    public DbSet<ArrangementFormField> ArrangementFormFields => Set<ArrangementFormField>();
    public DbSet<ArrangementShift> ArrangementShifts => Set<ArrangementShift>();
    public DbSet<ArrangementShiftRequirement> ArrangementShiftRequirements => Set<ArrangementShiftRequirement>();
    public DbSet<ArrangementAllowedPerson> ArrangementAllowedPersons => Set<ArrangementAllowedPerson>();
    public DbSet<ArrangementAllowedGroup> ArrangementAllowedGroups => Set<ArrangementAllowedGroup>();
    public DbSet<ArrangementRegistration> ArrangementRegistrations => Set<ArrangementRegistration>();
    public DbSet<ArrangementRegistrationAnswer> ArrangementRegistrationAnswers => Set<ArrangementRegistrationAnswer>();
    public DbSet<ArrangementRegistrationShift> ArrangementRegistrationShifts => Set<ArrangementRegistrationShift>();

    // Aktiviteter: planned club activities/events with a target audience (grupper), a workgroup
    // (plain contact registrations), a task list, and optional links to an Arrangement/Form
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityForm> ActivityForms => Set<ActivityForm>();
    public DbSet<ActivityTargetGroup> ActivityTargetGroups => Set<ActivityTargetGroup>();
    public DbSet<ActivityWorkgroupMember> ActivityWorkgroupMembers => Set<ActivityWorkgroupMember>();
    public DbSet<ActivityTask> ActivityTasks => Set<ActivityTask>();

    // Aktivitet "Filer"-fane: mapper, logiske filer og deres versioner (fysiske filer i app_files/activities)
    public DbSet<ActivityFolder> ActivityFolders => Set<ActivityFolder>();
    public DbSet<ActivityFile> ActivityFiles => Set<ActivityFile>();
    public DbSet<ActivityFileVersion> ActivityFileVersions => Set<ActivityFileVersion>();

    // Aktivitet "Lister"-fane: arbejdslister importeret fra Excel, med status/note/tilknyttet pr. linje
    public DbSet<ActivityList> ActivityLists => Set<ActivityList>();
    public DbSet<ActivityListColumn> ActivityListColumns => Set<ActivityListColumn>();
    public DbSet<ActivityListStatus> ActivityListStatuses => Set<ActivityListStatus>();
    public DbSet<ActivityListItem> ActivityListItems => Set<ActivityListItem>();
    public DbSet<ActivityListCellValue> ActivityListCellValues => Set<ActivityListCellValue>();
    public DbSet<ActivityListLabel> ActivityListLabels => Set<ActivityListLabel>();

    // Værktøjer → Labels: named sets of designed labels printed to A4 label sheets, with an image library each
    public DbSet<LabelCollection> LabelCollections => Set<LabelCollection>();
    public DbSet<LabelCollectionItem> LabelCollectionItems => Set<LabelCollectionItem>();
    public DbSet<LabelCollectionMedia> LabelCollectionMedia => Set<LabelCollectionMedia>();

    // Værktøjer → Infoskærme: slideshows played on projectors/screens via a public link
    public DbSet<InfoScreen> InfoScreens => Set<InfoScreen>();
    public DbSet<InfoScreenSlide> InfoScreenSlides => Set<InfoScreenSlide>();
    public DbSet<InfoScreenMedia> InfoScreenMedia => Set<InfoScreenMedia>();

    // Kommunikation: broadcast messages to Persons/PersonGroups over Email/SMS, with resolved
    // per-recipient audit trail (CommunicationMessageRecipient) and the email outbound queue
    public DbSet<CommunicationMessage> CommunicationMessages => Set<CommunicationMessage>();
    public DbSet<CommunicationMessageGroup> CommunicationMessageGroups => Set<CommunicationMessageGroup>();
    public DbSet<CommunicationMessageRecipientPerson> CommunicationMessageRecipientPersons => Set<CommunicationMessageRecipientPerson>();
    public DbSet<CommunicationMessageRecipient> CommunicationMessageRecipients => Set<CommunicationMessageRecipient>();
    public DbSet<CommunicationEmailMessage> CommunicationEmailMessages => Set<CommunicationEmailMessage>();
    public DbSet<CommunicationMessageAttachment> CommunicationMessageAttachments => Set<CommunicationMessageAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WebDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = now;
            if (entry.State is EntityState.Added or EntityState.Modified) entry.Entity.UpdatedAt = now;
        }
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
