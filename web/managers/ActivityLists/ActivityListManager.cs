using System.Globalization;
using web.Constants;
using web.Data.Models;
using web.Extensions;
using web.Managers.ActivityLists.Dtos;
using web.Managers.ActivityLists.Interfaces;
using web.Repositories.Activities.Interfaces;
using web.Repositories.ActivityLists.Dtos;
using web.Repositories.ActivityLists.Interfaces;
using web.Repositories.CommunicationEmailMessages.Dtos;
using web.Repositories.CommunicationEmailMessages.Interfaces;
using web.Repositories.SmsMessages.Dtos;
using web.Repositories.SmsMessages.Interfaces;

namespace web.Managers.ActivityLists;

public class ActivityListManager(
    IActivityListRepository activityListRepository,
    IActivityRepository activityRepository,
    ICommunicationEmailMessageRepository communicationEmailMessageRepository,
    ISmsMessageRepository smsMessageRepository,
    ILogger<ActivityListManager> logger) : IActivityListManager
{
    public async Task<ActivityListActionResultDto> CreateFromExcelAsync(CreateActivityListFromExcelDto dto, CancellationToken cancellationToken = default)
    {
        if (!await activityRepository.ExistsAsync(dto.ActivityId, cancellationToken))
            return ActivityListActionResultDto.Fail("Aktiviteten blev ikke fundet.");

        if (!string.Equals(Path.GetExtension(dto.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return ActivityListActionResultDto.Fail("Vælg en Excel-fil i formatet .xlsx.");

        var import = ActivityListExcel.Parse(dto.Content);
        if (!import.Success)
            return ActivityListActionResultDto.Fail(import.ErrorMessage!);
        if (import.Rows.Count == 0)
            return ActivityListActionResultDto.Fail("Arket har ingen linjer under overskriftsrækken.");

        return await activityListRepository.CreateAsync(new CreateActivityListDto
        {
            ActivityId = dto.ActivityId,
            Title = dto.Title,
            Description = dto.Description,
            SourceFileName = Path.GetFileName(dto.FileName),
            Headers = import.Headers,
            Rows = import.Rows,
            CreatedByUserId = dto.UserId
        }, cancellationToken);
    }

    public async Task<ActivityListExportDto?> ExportAsync(SearchActivityListItemsDto search, string? userId, CancellationToken cancellationToken = default)
    {
        var data = await activityListRepository.GetExportDataAsync(search, userId, cancellationToken);
        if (data is null)
            return null;

        var schema = data.Schema;
        var statuses = schema.Statuses.ToDictionary(s => s.Id);
        var members = schema.Members.ToDictionary(m => m.Id, m => m.Name);

        var headers = new List<string> { "Nr." };
        headers.AddRange(schema.Columns.Select(c => c.Name));
        var statusColumn = headers.Count;
        headers.Add("Status");
        headers.Add("Tilknyttet");
        if (schema.ShowNote)
            headers.Add("Note");
        headers.Add("Sidst ændret");
        headers.Add("Ændret af");

        var lines = data.Rows.Select(r =>
        {
            var status = r.StatusId.HasValue ? statuses.GetValueOrDefault(r.StatusId.Value) : null;
            var cells = new List<string?> { r.RowNumber.ToString(CultureInfo.InvariantCulture) };
            cells.AddRange(schema.Columns.Select(c =>
            {
                var value = r.Values.GetValueOrDefault(c.Id);
                return c.Kind == ActivityListColumnKind.YesNo ? (value == "true" ? "Ja" : "Nej") : value;
            }));
            cells.Add(status?.Name);
            cells.Add(r.AssignedMemberId.HasValue ? members.GetValueOrDefault(r.AssignedMemberId.Value) : null);
            if (schema.ShowNote)
                cells.Add(r.Note);
            cells.Add(r.UpdatedAtUtc?.ToLocalTime().ToDanishDateTime());
            cells.Add(r.UpdatedByName);
            return (cells.ToArray(), status?.Color);
        });

        var content = ActivityListExcel.Build(data.Title, headers, lines, statusColumn);

        // A per-person export gets the name in the file name, so it can be sent straight on.
        var suffix = search.Assigned switch
        {
            ActivityListRules.AssignedMine => " - mine",
            ActivityListRules.AssignedNone => " - uden tilknytning",
            { } id when int.TryParse(id, out var memberId) && members.TryGetValue(memberId, out var name) => $" - {name}",
            _ => search.HasFilter ? " - filtreret" : string.Empty
        };

        return new ActivityListExportDto
        {
            Content = content,
            FileName = FileNameHelper.Sanitize($"{data.Title}{suffix}", "liste") + ".xlsx"
        };
    }

    public async Task<ActivityListActionResultDto> SendLinksAsync(SendActivityListLinksDto dto, string baseUrl, CancellationToken cancellationToken = default)
    {
        if (!dto.ViaEmail && !dto.ViaSms)
            return ActivityListActionResultDto.Fail("Vælg e-mail og/eller SMS.");

        var recipients = await activityListRepository.GetLinkRecipientsAsync(dto.ListId, dto.MemberIds, cancellationToken);
        if (recipients is null)
            return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");
        if (recipients.Members.Count == 0)
            return ActivityListActionResultDto.Fail("Vælg mindst én ekstern kontakt fra arbejdsgruppen.");

        var message = string.IsNullOrWhiteSpace(dto.Message) ? null : dto.Message.Trim();
        var subject = $"{recipients.ActivityTitle}: {recipients.ListTitle}";
        var emails = new List<QueueCommunicationEmailDto>();
        var sms = new List<QueueSmsMessageDto>();
        var missing = new List<string>();

        foreach (var member in recipients.Members)
        {
            var count = member.AssignedCount;
            var body = $"Hej {member.Name}\n\n"
                + (message is null ? string.Empty : $"{message}\n\n")
                + $"Du har {count} linje{(count == 1 ? "" : "r")} på listen \"{recipients.ListTitle}\" ({recipients.ActivityTitle}). "
                + $"Åbn dine linjer her:\n{ActivityListRules.BuildPublicLink(baseUrl, recipients.ListId, member.PublicId)}";

            var sent = false;
            if (dto.ViaEmail && !string.IsNullOrWhiteSpace(member.Email))
            {
                emails.Add(new QueueCommunicationEmailDto
                {
                    ToAddress = member.Email.Trim(),
                    Subject = subject,
                    Body = body,
                    HtmlBody = EmailHtmlHelper.FromPlainText(body)
                });
                sent = true;
            }
            if (dto.ViaSms && !string.IsNullOrWhiteSpace(member.Mobile))
            {
                sms.Add(new QueueSmsMessageDto { PhoneNumber = member.Mobile.Trim(), Body = body });
                sent = true;
            }
            if (!sent)
                missing.Add(member.Name);
        }

        if (emails.Count + sms.Count == 0)
            return ActivityListActionResultDto.Fail($"Ingen af de valgte har {(dto.ViaEmail && dto.ViaSms ? "e-mail eller mobilnummer" : dto.ViaEmail ? "en e-mail" : "et mobilnummer")} registreret.");

        // Picked up and sent by CommunicationEmailWorker / SmsWorker.
        if (emails.Count > 0)
            await communicationEmailMessageRepository.QueueAsync(emails, cancellationToken);
        if (sms.Count > 0)
            await smsMessageRepository.QueueAsync(sms, cancellationToken);

        logger.LogInformation("List {ListId}: queued work list links to {Members} member(s) ({Emails} e-mail, {Sms} SMS)",
            recipients.ListId, recipients.Members.Count - missing.Count, emails.Count, sms.Count);

        var parts = new List<string>();
        if (emails.Count > 0) parts.Add($"{emails.Count} e-mail{(emails.Count == 1 ? "" : "s")}");
        if (sms.Count > 0) parts.Add($"{sms.Count} SMS");
        var text = $"Links sendes: {string.Join(" og ", parts)}.";
        if (missing.Count > 0)
            text += $" Mangler kontaktinfo: {string.Join(", ", missing)}.";
        return ActivityListActionResultDto.Ok(recipients.ListId, text);
    }
}
