using web.Constants;
using web.Extensions;
using web.Managers.Communication.Dtos;
using web.Managers.Communication.Interfaces;
using web.Repositories.Arrangements.Interfaces;
using web.Repositories.CommunicationMessages.Dtos;
using web.Repositories.CommunicationMessages.Interfaces;
using web.Repositories.Files.Dtos;
using web.Repositories.Files.Interfaces;
using web.Repositories.Forms.Interfaces;
using web.Repositories.People.Dtos;
using web.Repositories.People.Interfaces;
using web.Repositories.PersonGroups.Interfaces;

namespace web.Managers.Communication;

public class CommunicationManager(
    ICommunicationMessageRepository communicationMessageRepository,
    IPersonRepository personRepository,
    IPersonGroupRepository personGroupRepository,
    IFormRepository formRepository,
    IArrangementRepository arrangementRepository,
    IFileRepository fileRepository,
    ILogger<CommunicationManager> logger) : ICommunicationManager
{
    public async Task<ComposeOptionsDto> GetComposeOptionsAsync(CancellationToken cancellationToken = default)
    {
        var arrangements = await arrangementRepository.GetLinkOptionsAsync(cancellationToken);

        return new ComposeOptionsDto
        {
            Groups = await personGroupRepository.GetAllAsync(cancellationToken),
            People = await personRepository.GetOptionsAsync(cancellationToken),
            Forms = await formRepository.GetLatestVersionsAsync(acceptingResponsesOnly: true, cancellationToken),
            // Closed arrangements drop out of the picker entirely — only not-yet-open and open
            // ones are valid link targets (mirrors the admin Tilmelding list's status).
            Arrangements = arrangements
                .Select(a => new ComposeArrangementOptionDto
                {
                    Arrangement = a,
                    Status = ArrangementRegistrationStatuses.GetStatus(a.RegistrationOpensAtUtc, a.RegistrationClosesAtUtc, a.RegistrationForcedOpen)
                })
                .Where(a => a.Status != ArrangementRegistrationStatus.Closed)
                .ToList()
        };
    }

    public async Task<CommunicationMessageListDto> GetMessagesAsync(int? activityId, CancellationToken cancellationToken = default) => new()
    {
        Messages = await communicationMessageRepository.GetListAsync(activityId, cancellationToken),
        TotalGroupCount = await personGroupRepository.CountAsync(cancellationToken)
    };

    public async Task<CommunicationMessageDetailsDto?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await communicationMessageRepository.GetByIdAsync(id, cancellationToken);
        if (message is null)
        {
            return null;
        }

        return new CommunicationMessageDetailsDto
        {
            Message = message,
            TotalGroupCount = await personGroupRepository.CountAsync(cancellationToken),
            Recipients = await BuildRecipientPageAsync(message, new SearchCommunicationRecipientsDto(), cancellationToken)
        };
    }

    public async Task<CommunicationRecipientPageDto?> GetRecipientsAsync(int id, SearchCommunicationRecipientsDto search, CancellationToken cancellationToken = default)
    {
        var message = await communicationMessageRepository.GetByIdAsync(id, cancellationToken);
        return message is null ? null : await BuildRecipientPageAsync(message, search, cancellationToken);
    }

    public async Task<List<CommunicationResendTargetDto>> GetResendTargetsAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await communicationMessageRepository.GetByIdAsync(id, cancellationToken);
        if (message is null)
        {
            return [];
        }

        var receivedBefore = (await communicationMessageRepository.GetRecipientsAsync(id, cancellationToken))
            .Select(r => r.PersonId)
            .ToHashSet();
        var audience = await GetAudienceByNameAsync(message, cancellationToken);

        return audience
            .Select(p => new CommunicationResendTargetDto { PersonId = p.Id, Name = p.Name, ReceivedBefore = receivedBefore.Contains(p.Id) })
            .ToList();
    }

    public async Task<SendCommunicationResultDto> SaveAsync(SaveCommunicationDto dto, string baseUrl, CancellationToken cancellationToken = default)
    {
        var subject = dto.Subject?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result(SendCommunicationStatus.SubjectRequired);
        }

        if (!dto.ViaEmail && !dto.ViaSms)
        {
            return Result(SendCommunicationStatus.ChannelRequired);
        }

        var groupIds = dto.GroupIds.Distinct().ToList();
        var personIds = dto.PersonIds.Distinct().ToList();

        // Defense in depth: the compose modal's JS already restricts the recipient pickers to
        // an attached Restricted arrangement's own allow-list, but that's client-side only —
        // re-apply the same filter here so a personal Tilmelding link can never be saved for
        // someone who isn't actually allowed to sign up.
        if (dto.ArrangementId.HasValue)
        {
            var arrangement = await arrangementRepository.GetByIdAsync(dto.ArrangementId.Value, cancellationToken);
            if (arrangement is not null && arrangement.AccessMode == ArrangementAccessMode.Restricted)
            {
                groupIds = groupIds.Where(arrangement.AllowedGroupIds.Contains).ToList();
                personIds = personIds.Where(arrangement.AllowedPersonIds.Contains).ToList();
            }
        }

        if (groupIds.Count == 0 && personIds.Count == 0)
        {
            return Result(SendCommunicationStatus.RecipientRequired);
        }

        var audience = await personRepository.GetContactsAsync(groupIds, personIds, cancellationToken);
        var saved = await communicationMessageRepository.SaveDraftAsync(new SaveCommunicationMessageDto
        {
            Id = dto.Id,
            Subject = subject,
            Body = dto.Body?.Trim() ?? string.Empty,
            ViaEmail = dto.ViaEmail,
            ViaSms = dto.ViaSms,
            FormId = dto.FormId,
            LinkType = dto.FormId.HasValue ? (dto.LinkType == "personal" ? "personal" : "shared") : null,
            ArrangementId = dto.ArrangementId,
            ActivityId = dto.ActivityId,
            GroupIds = groupIds,
            PersonIds = personIds,
            RecipientCount = audience.Count,
            RecipientSummary = await BuildRecipientSummaryAsync(groupIds, personIds, cancellationToken),
            CreatedByUserId = dto.UserId
        }, cancellationToken);

        switch (saved.Status)
        {
            case SaveCommunicationMessageStatus.NotFound:
                return Result(SendCommunicationStatus.NotFound);
            case SaveCommunicationMessageStatus.NotDraft:
                return Result(SendCommunicationStatus.NotDraft);
        }

        // Attachments only ever go out over e-mail, never SMS — ignored (rather than rejected)
        // if ViaEmail isn't set, since the compose modal's file input is only reachable then.
        if (dto.ViaEmail)
        {
            foreach (var attachment in dto.Attachments)
            {
                await AddAttachmentAsync(saved.Id, attachment, dto.UserId, cancellationToken);
            }
        }

        return dto.Send
            ? await SendAsync(saved.Id, baseUrl, null, cancellationToken)
            : Result(SendCommunicationStatus.Saved, saved.Id);
    }

    public Task<SendCommunicationResultDto> SendExistingAsync(int id, string baseUrl, IReadOnlyCollection<int>? recipientPersonIds, CancellationToken cancellationToken = default) =>
        SendAsync(id, baseUrl, recipientPersonIds, cancellationToken);

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var fileIds = await communicationMessageRepository.DeleteAsync(id, cancellationToken);
        if (fileIds is null)
        {
            return false;
        }

        foreach (var fileId in fileIds)
        {
            await fileRepository.DeleteAsync(fileId, cancellationToken);
        }

        return true;
    }

    public async Task<StoredFileDto?> GetAttachmentFileAsync(int attachmentId, CancellationToken cancellationToken = default)
    {
        var fileId = await communicationMessageRepository.GetAttachmentFileIdAsync(attachmentId, cancellationToken);
        return fileId is null ? null : await fileRepository.GetByIdAsync(fileId.Value, cancellationToken);
    }

    private async Task<SendCommunicationResultDto> SendAsync(int id, string baseUrl, IReadOnlyCollection<int>? recipientPersonIdsOverride, CancellationToken cancellationToken)
    {
        var message = await communicationMessageRepository.GetByIdAsync(id, cancellationToken);
        if (message is null)
        {
            return Result(SendCommunicationStatus.NotFound);
        }

        // Covers every path that actually dispatches a message — first send, sending a saved
        // draft, and "Send igen" (full or partial) — so an empty body can never go out to a
        // whole group/audience regardless of which button triggered it.
        if (string.IsNullOrWhiteSpace(message.Body))
        {
            return Result(SendCommunicationStatus.EmptyBody, message.Id);
        }

        var groupIds = message.Groups.Select(g => g.Id).ToList();
        var personIds = message.DirectPersonIds;

        // A picked subset from the "Send igen" modal narrows the send down to specific persons
        // instead of the whole configured audience — but only ones actually within that
        // audience, re-checked here server-side so the picker can't be used to reach anyone else.
        var isPartialSend = recipientPersonIdsOverride is { Count: > 0 };
        List<PersonContactDto> targets;
        if (isPartialSend)
        {
            var allowedIds = (await personRepository.GetContactsAsync(groupIds, personIds, cancellationToken)).Select(p => p.Id).ToHashSet();
            var pickedIds = recipientPersonIdsOverride!.Where(allowedIds.Contains).Distinct().ToList();
            targets = await personRepository.GetContactsAsync([], pickedIds, cancellationToken);
        }
        else
        {
            targets = await personRepository.GetContactsAsync(groupIds, personIds, cancellationToken);
        }

        var resolved = ResolveRecipients(targets, message.ViaEmail, message.ViaSms);
        if (resolved.Count == 0)
        {
            logger.LogWarning("CommunicationMessage {Id} resolved to zero recipients", message.Id);
            return Result(SendCommunicationStatus.NoRecipients, message.Id);
        }

        var form = message.FormId.HasValue ? await formRepository.GetLatestLinkAsync(message.FormId.Value, cancellationToken) : null;
        // Non-anonymous forms always require a personal link server-side, regardless of what
        // the client sent — mirrors the compose modal's own JS rule.
        var usePersonalFormLink = form is not null && (!form.IsAnonymous || message.LinkType == "personal");
        var sharedFormLink = form is not null && !usePersonalFormLink ? $"{baseUrl}/Formular?Id={form.PublicId}" : null;

        // Tilmelding has no anonymous mode — the link is always personal.
        var arrangement = message.ArrangementId.HasValue ? await arrangementRepository.GetByIdAsync(message.ArrangementId.Value, cancellationToken) : null;

        var publicIds = targets.ToDictionary(p => p.Id, p => p.PublicId);
        var queued = new List<QueuedCommunicationRecipientDto>();
        foreach (var recipient in resolved)
        {
            var body = message.Body.Replace("{{Navn}}", recipient.DisplayName);

            if (form is not null)
            {
                var link = usePersonalFormLink && publicIds.TryGetValue(recipient.PersonId, out var formPersonPublicId)
                    ? $"{baseUrl}/Formular?Id={form.PublicId}&UId={formPersonPublicId}"
                    : sharedFormLink;

                if (link is not null)
                {
                    body = $"{body}\n\n{form.Title}: {link}";
                }
            }

            if (arrangement is not null && publicIds.TryGetValue(recipient.PersonId, out var arrangementPersonPublicId))
            {
                body = $"{body}\n\n{arrangement.Title}: {baseUrl}/Tilmelding?Id={arrangement.PublicId}&UId={arrangementPersonPublicId}";
            }

            recipient.Body = body;
            recipient.HtmlBody = recipient.Channel == CommunicationChannel.Email ? EmailHtmlHelper.FromPlainText(body) : null;
            queued.Add(recipient);
        }

        // A partial "Send igen" targets a subset of the configured audience for this one send —
        // it doesn't redefine the message's audience, so the stats keep describing the full
        // groups/persons selection instead of shrinking to whoever this particular send reached.
        await communicationMessageRepository.MarkSentAsync(new MarkCommunicationMessageSentDto
        {
            MessageId = message.Id,
            Recipients = queued,
            RecipientCount = isPartialSend ? null : resolved.Select(r => r.PersonId).Distinct().Count(),
            RecipientSummary = isPartialSend ? null : await BuildRecipientSummaryAsync(groupIds, personIds, cancellationToken)
        }, cancellationToken);

        return Result(SendCommunicationStatus.Sent, message.Id);
    }

    /// <summary>
    /// The deduped (Channel, Address) send targets: each person's own contact info plus their
    /// guardians', for the enabled channels. Persons are processed by Id and the first one to
    /// claim an address wins, so a shared address (e.g. a parent of two) gets one message.
    /// </summary>
    private static List<QueuedCommunicationRecipientDto> ResolveRecipients(List<PersonContactDto> targets, bool viaEmail, bool viaSms)
    {
        var seen = new HashSet<(string Channel, string Normalized)>();
        var result = new List<QueuedCommunicationRecipientDto>();
        if (!viaEmail && !viaSms)
        {
            return result;
        }

        void TryAdd(PersonContactDto person, string channel, string? rawAddress)
        {
            if (string.IsNullOrWhiteSpace(rawAddress))
            {
                return;
            }

            var address = rawAddress.Trim();
            var normalized = channel == CommunicationChannel.Email
                ? address.ToLowerInvariant()
                : new string(address.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());

            if (normalized.Length == 0 || !seen.Add((channel, normalized)))
            {
                return;
            }

            result.Add(new QueuedCommunicationRecipientDto { PersonId = person.Id, DisplayName = person.Name, Channel = channel, Address = address });
        }

        foreach (var person in targets)
        {
            if (viaEmail) TryAdd(person, CommunicationChannel.Email, person.Email);
            if (viaSms) TryAdd(person, CommunicationChannel.Sms, person.Mobile);

            foreach (var guardian in person.Guardians)
            {
                if (viaEmail) TryAdd(person, CommunicationChannel.Email, guardian.Email);
                if (viaSms) TryAdd(person, CommunicationChannel.Sms, guardian.Mobile);
            }
        }

        return result;
    }

    private async Task<CommunicationRecipientPageDto> BuildRecipientPageAsync(CommunicationMessageDto message, SearchCommunicationRecipientsDto search, CancellationToken cancellationToken)
    {
        // The full target audience — every person covered by the selected groups/persons — not
        // just the ones that ended up with a resolvable address. People with no email/mobile at
        // all (own or via a guardian) still belong on this list, flagged as missing contact
        // info, so gaps in the club's contact data are visible instead of silently disappearing.
        var audience = await GetAudienceByNameAsync(message, cancellationToken);
        var recipientsByPerson = (await communicationMessageRepository.GetRecipientsAsync(message.Id, cancellationToken)).ToLookup(r => r.PersonId);

        var rows = audience.Select(person =>
        {
            var personRecipients = recipientsByPerson[person.Id];
            return new CommunicationRecipientRowDto
            {
                DisplayName = person.Name,
                EmailAddresses = personRecipients
                    .Where(r => r.Channel == CommunicationChannel.Email)
                    .Select(r => new CommunicationRecipientAddressDto
                    {
                        Address = r.Address,
                        DeliveryStatus = (r.EmailStatus ?? CommunicationEmailMessageStatus.Pending).ToString()
                    })
                    .ToList(),
                HasEmailContact = !string.IsNullOrWhiteSpace(person.Email) || person.Guardians.Any(g => !string.IsNullOrWhiteSpace(g.Email)),
                SmsAddresses = personRecipients
                    .Where(r => r.Channel == CommunicationChannel.Sms)
                    .Select(r => new CommunicationRecipientAddressDto
                    {
                        Address = r.Address,
                        DeliveryStatus = r.SmsStatus ?? SmsMessageStatus.Pending
                    })
                    .ToList(),
                HasSmsContact = !string.IsNullOrWhiteSpace(person.Mobile) || person.Guardians.Any(g => !string.IsNullOrWhiteSpace(g.Mobile))
            };
        });

        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            var term = search.SearchText.Trim();
            rows = rows.Where(r => r.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        rows = search.ContactStatus switch
        {
            "MissingEmail" => rows.Where(r => !r.HasEmailContact),
            "MissingSms" => rows.Where(r => !r.HasSmsContact),
            "Sent" => rows.Where(r =>
                r.EmailAddresses.Any(a => a.DeliveryStatus == CommunicationEmailMessageStatus.Sent.ToString())
                || r.SmsAddresses.Any(a => a.DeliveryStatus == SmsMessageStatus.Sent)),
            _ => rows
        };

        var filtered = rows.ToList();
        var page = search.Page < 1 ? 1 : search.Page;
        var pageSize = search.PageSize is < 5 or > 200 ? 10 : search.PageSize;

        return new CommunicationRecipientPageDto
        {
            Items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            TotalCount = filtered.Count,
            Page = page,
            PageSize = pageSize,
            ViaEmail = message.ViaEmail,
            ViaSms = message.ViaSms,
            IsSent = message.SentAtUtc.HasValue
        };
    }

    /// <summary>Everyone covered by the message's groups/persons, ordered by name (ordinal, like SQLite's ORDER BY).</summary>
    private async Task<List<PersonContactDto>> GetAudienceByNameAsync(CommunicationMessageDto message, CancellationToken cancellationToken) =>
        (await personRepository.GetContactsAsync(message.Groups.Select(g => g.Id).ToList(), message.DirectPersonIds, cancellationToken))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();

    private async Task<string> BuildRecipientSummaryAsync(IReadOnlyCollection<int> groupIds, IReadOnlyCollection<int> personIds, CancellationToken cancellationToken)
    {
        var parts = new List<string>();

        if (groupIds.Count > 0)
        {
            var groups = await personGroupRepository.GetAllAsync(cancellationToken);
            parts.AddRange(groups.Where(g => groupIds.Contains(g.Id)).Select(g => g.Name));
        }

        if (personIds.Count > 0)
        {
            parts.Add(personIds.Count == 1 ? "1 person" : $"{personIds.Count} personer");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "Ingen modtagere";
    }

    /// <summary>Stores an e-mail attachment in app_files/communication/ and links it to the message.</summary>
    private async Task AddAttachmentAsync(int messageId, CommunicationAttachmentUploadDto attachment, string? userId, CancellationToken cancellationToken)
    {
        var stored = await fileRepository.CreateAsync(new CreateStoredFileDto
        {
            OriginalFileName = attachment.FileName,
            ContentType = attachment.ContentType,
            Category = FileCategories.Communication,
            OwnerId = userId
        }, attachment.Content, cancellationToken);

        if (!await communicationMessageRepository.AddAttachmentAsync(messageId, stored.Id, cancellationToken))
        {
            await fileRepository.DeleteAsync(stored.Id, cancellationToken);
        }
    }

    private static SendCommunicationResultDto Result(SendCommunicationStatus status, int messageId = 0) => new() { Status = status, MessageId = messageId };
}
