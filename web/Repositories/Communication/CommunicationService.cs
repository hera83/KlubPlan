using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.Communication.Dtos;
using web.Repositories.Communication.Interfaces;
using web.ViewModels;

namespace web.Repositories.Communication
{
    public class CommunicationService : ICommunicationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommunicationService> _logger;

        public CommunicationService(ApplicationDbContext context, ILogger<CommunicationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CommunicationIndexViewModel> GetIndexDataAsync(bool isAdmin, CancellationToken ct = default)
        {
            var groups = await _context.PersonGroups
                .OrderBy(g => g.Name)
                .Select(g => new PersonGroupOptionViewModel { Id = g.Id, Name = g.Name })
                .ToListAsync(ct);

            var people = await _context.People
                .OrderBy(p => p.Name)
                .Select(p => new ArrangementPersonOptionViewModel { Id = p.Id, Uid = p.Uid, Name = p.Name })
                .ToListAsync(ct);

            var forms = await _context.Forms.AsNoTracking().ToListAsync(ct);
            var formOptions = forms
                .GroupBy(f => f.RootFormId ?? f.Id)
                .Select(g => g.OrderByDescending(f => f.VersionNumber).First())
                .Where(f => f.IsAcceptingResponses)
                .OrderBy(f => f.Title)
                .Select(f => new CommunicationFormOptionViewModel { Id = f.Id, Title = f.Title, IsAnonymous = f.IsAnonymous })
                .ToList();

            var arrangementRows = await _context.Arrangements
                .AsNoTracking()
                .OrderBy(a => a.Title)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    IsRestricted = a.AccessMode == ArrangementAccessMode.Restricted,
                    AllowedPersonIds = a.AllowedPersons.Select(p => p.PersonId).ToList(),
                    AllowedGroupIds = a.AllowedGroups.Select(g => g.PersonGroupId).ToList(),
                    a.RegistrationOpensAtUtc,
                    a.RegistrationClosesAtUtc,
                    a.RegistrationForcedOpen
                })
                .ToListAsync(ct);

            // Closed arrangements drop out of the picker entirely — only not-yet-open and open
            // ones are valid link targets (mirrors the admin Tilmelding list's status).
            var arrangementOptions = arrangementRows
                .Select(a => new
                {
                    a,
                    Status = ArrangementRegistrationStatuses.GetStatus(a.RegistrationOpensAtUtc, a.RegistrationClosesAtUtc, a.RegistrationForcedOpen)
                })
                .Where(x => x.Status != ArrangementRegistrationStatus.Closed)
                .Select(x => new CommunicationArrangementOptionViewModel
                {
                    Id = x.a.Id,
                    Title = x.a.Title,
                    IsRestricted = x.a.IsRestricted,
                    AllowedPersonIds = x.a.AllowedPersonIds,
                    AllowedGroupIds = x.a.AllowedGroupIds,
                    DefaultLinkText = BuildArrangementDefaultLinkText(x.Status, x.a.RegistrationOpensAtUtc)
                })
                .ToList();

            var messageEntities = await _context.CommunicationMessages
                .AsNoTracking()
                .Include(m => m.Groups).ThenInclude(g => g.PersonGroup)
                .Include(m => m.DirectPersons).ThenInclude(dp => dp.Person)
                .OrderByDescending(m => m.CreatedAtUtc)
                .ToListAsync(ct);

            var messages = messageEntities.Select(m => new CommunicationMessageListItemViewModel
            {
                Id = m.Id,
                Subject = m.Subject,
                ViaEmail = m.ViaEmail,
                ViaSms = m.ViaSms,
                RecipientBadges = BuildRecipientBadges(m),
                RecipientCount = m.RecipientCount,
                Status = CommunicationMessageStatuses.GetUILabel(m.Status),
                StatusBadgeClass = CommunicationMessageStatuses.GetBadgeClass(m.Status),
                IsDraft = m.Status == CommunicationMessageStatus.Draft,
                SentAtUtc = m.SentAtUtc,
                CreatedAtUtc = m.CreatedAtUtc
            }).ToList();

            return new CommunicationIndexViewModel
            {
                IsAdmin = isAdmin,
                Messages = messages,
                GroupOptions = groups,
                PersonOptions = people,
                FormOptions = formOptions,
                ArrangementOptions = arrangementOptions
            };
        }

        public async Task<CommunicationMessageDetailViewModel?> GetDetailsAsync(int id, CancellationToken ct = default)
        {
            var message = await _context.CommunicationMessages
                .AsNoTracking()
                .Include(m => m.Form)
                .Include(m => m.Arrangement)
                .Include(m => m.Groups).ThenInclude(g => g.PersonGroup)
                .Include(m => m.DirectPersons).ThenInclude(dp => dp.Person)
                .Include(m => m.Recipients).ThenInclude(r => r.SmsMessage)
                .Include(m => m.Recipients).ThenInclude(r => r.CommunicationEmailMessage)
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            if (message is null)
            {
                return null;
            }

            var groupIds = message.Groups.Select(g => g.PersonGroupId).ToList();
            var personIds = message.DirectPersons.Select(p => p.PersonId).ToList();

            // The full target audience — every person covered by the selected groups/persons —
            // not just the ones that ended up with a resolvable address. People with no email/
            // mobile at all (own or via a guardian) still belong on this list, flagged as missing
            // contact info, so gaps in the club's contact data are visible instead of silently
            // disappearing.
            var targets = await _context.People
                .AsNoTracking()
                .Where(p => personIds.Contains(p.Id) || p.Memberships.Any(m => groupIds.Contains(m.GroupId)))
                .OrderBy(p => p.Name)
                .Include(p => p.Guardians)
                .ToListAsync(ct);

            var recipientsByPerson = message.Recipients.ToLookup(r => r.PersonId);

            var recipients = targets.Select(person =>
            {
                var personRecipients = recipientsByPerson[person.Id];

                return new CommunicationRecipientDetailViewModel
                {
                    DisplayName = person.Name,
                    EmailAddresses = personRecipients
                        .Where(r => r.Channel == CommunicationChannel.Email)
                        .Select(r => new CommunicationRecipientAddressViewModel
                        {
                            Address = r.Address,
                            DeliveryStatus = r.CommunicationEmailMessage?.Status.ToString() ?? CommunicationEmailMessageStatus.Pending.ToString()
                        })
                        .ToList(),
                    HasEmailContact = !string.IsNullOrWhiteSpace(person.Email) || person.Guardians.Any(g => !string.IsNullOrWhiteSpace(g.Email)),
                    SmsAddresses = personRecipients
                        .Where(r => r.Channel == CommunicationChannel.Sms)
                        .Select(r => new CommunicationRecipientAddressViewModel
                        {
                            Address = r.Address,
                            DeliveryStatus = r.SmsMessage?.Status ?? SmsMessageStatus.Pending
                        })
                        .ToList(),
                    HasSmsContact = !string.IsNullOrWhiteSpace(person.Mobile) || person.Guardians.Any(g => !string.IsNullOrWhiteSpace(g.Mobile))
                };
            }).ToList();

            return new CommunicationMessageDetailViewModel
            {
                Id = message.Id,
                Subject = message.Subject,
                Body = message.Body,
                Status = CommunicationMessageStatuses.GetUILabel(message.Status),
                StatusBadgeClass = CommunicationMessageStatuses.GetBadgeClass(message.Status),
                RecipientSummary = message.RecipientSummary,
                RecipientBadges = BuildRecipientBadges(message),
                IsSent = message.SentAtUtc.HasValue,
                ViaEmail = message.ViaEmail,
                ViaSms = message.ViaSms,
                CreatedAtUtc = message.CreatedAtUtc,
                SentAtUtc = message.SentAtUtc,
                FormTitle = message.Form?.Title,
                ArrangementTitle = message.Arrangement?.Title,
                Recipients = recipients
            };
        }

        public async Task<List<ResolvedRecipientDto>> ResolveRecipientsAsync(
            IReadOnlyCollection<int> groupIds,
            IReadOnlyCollection<int> personIds,
            bool viaEmail,
            bool viaSms,
            CancellationToken ct = default)
        {
            if (!viaEmail && !viaSms)
            {
                return new List<ResolvedRecipientDto>();
            }

            if (groupIds.Count == 0 && personIds.Count == 0)
            {
                return new List<ResolvedRecipientDto>();
            }

            // Union of directly-selected persons and members of selected groups; EF/SQL already
            // collapses this to distinct People rows.
            var targets = await _context.People
                .Where(p => personIds.Contains(p.Id) || p.Memberships.Any(m => groupIds.Contains(m.GroupId)))
                .OrderBy(p => p.Id)
                .Include(p => p.Guardians)
                .ToListAsync(ct);

            var seen = new HashSet<(string Channel, string Normalized)>();
            var result = new List<ResolvedRecipientDto>();

            void TryAdd(Person person, string channel, string? rawAddress)
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
                    // Either empty after normalization, or a (channel, address) pair already
                    // claimed by an earlier-processed target Person — first match wins, so this
                    // Person is simply not attributed a duplicate send to a shared address.
                    return;
                }

                result.Add(new ResolvedRecipientDto(person.Id, person.Name, channel, address));
            }

            foreach (var person in targets)
            {
                if (viaEmail) TryAdd(person, CommunicationChannel.Email, person.Email);
                if (viaSms) TryAdd(person, CommunicationChannel.Sms, person.Mobile);

                foreach (var guardian in person.Guardians.OrderBy(g => g.Order))
                {
                    if (viaEmail) TryAdd(person, CommunicationChannel.Email, guardian.Email);
                    if (viaSms) TryAdd(person, CommunicationChannel.Sms, guardian.Mobile);
                }
            }

            return result;
        }

        public async Task<SaveMessageResponseDto> SaveMessageAsync(ComposeMessageRequestDto dto, string? userId, string baseUrl, CancellationToken ct = default)
        {
            var subject = dto.Subject?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(subject))
            {
                return new SaveMessageResponseDto { Success = false, ErrorMessage = "Emne skal udfyldes." };
            }

            if (!dto.ViaEmail && !dto.ViaSms)
            {
                return new SaveMessageResponseDto { Success = false, ErrorMessage = "Vælg mindst én kanal (e-mail eller SMS)." };
            }

            var groupIds = dto.GroupIds.Distinct().ToList();
            var personIds = dto.AllowedPersonIds.Distinct().ToList();

            // Defense in depth: the compose modal's JS already restricts the recipient pickers to
            // an attached Restricted arrangement's own allow-list, but that's client-side only —
            // re-apply the same filter here so a personal Tilmelding link can never be saved for
            // someone who isn't actually allowed to sign up.
            if (dto.ArrangementId.HasValue)
            {
                var arrangement = await _context.Arrangements
                    .AsNoTracking()
                    .Include(a => a.AllowedPersons)
                    .Include(a => a.AllowedGroups)
                    .FirstOrDefaultAsync(a => a.Id == dto.ArrangementId.Value, ct);

                if (arrangement is not null && arrangement.AccessMode == ArrangementAccessMode.Restricted)
                {
                    var allowedGroupIds = arrangement.AllowedGroups.Select(g => g.PersonGroupId).ToHashSet();
                    var allowedPersonIds = arrangement.AllowedPersons.Select(p => p.PersonId).ToHashSet();
                    groupIds = groupIds.Where(allowedGroupIds.Contains).ToList();
                    personIds = personIds.Where(allowedPersonIds.Contains).ToList();
                }
            }

            if (groupIds.Count == 0 && personIds.Count == 0)
            {
                return new SaveMessageResponseDto { Success = false, ErrorMessage = "Vælg mindst én modtager (gruppe eller person)." };
            }

            CommunicationMessage message;
            if (dto.Id > 0)
            {
                var existing = await _context.CommunicationMessages
                    .Include(m => m.Groups)
                    .Include(m => m.DirectPersons)
                    .FirstOrDefaultAsync(m => m.Id == dto.Id, ct);

                if (existing is null)
                {
                    return new SaveMessageResponseDto { Success = false, ErrorMessage = "Beskeden blev ikke fundet." };
                }

                if (existing.Status != CommunicationMessageStatus.Draft)
                {
                    return new SaveMessageResponseDto { Success = false, ErrorMessage = "Kun kladder kan redigeres." };
                }

                message = existing;
                _context.CommunicationMessageGroups.RemoveRange(existing.Groups);
                _context.CommunicationMessageRecipientPersons.RemoveRange(existing.DirectPersons);
                existing.Groups.Clear();
                existing.DirectPersons.Clear();
                message.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                message = new CommunicationMessage
                {
                    CreatedByUserId = userId,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _context.CommunicationMessages.Add(message);
            }

            message.Subject = subject;
            message.Body = dto.Body?.Trim() ?? string.Empty;
            message.ViaEmail = dto.ViaEmail;
            message.ViaSms = dto.ViaSms;
            message.FormId = dto.FormId;
            message.LinkType = dto.FormId.HasValue ? (dto.LinkType == "personal" ? "personal" : "shared") : null;
            message.ArrangementId = dto.ArrangementId;
            message.Status = CommunicationMessageStatus.Draft;

            foreach (var groupId in groupIds)
            {
                message.Groups.Add(new CommunicationMessageGroup { PersonGroupId = groupId });
            }

            foreach (var personId in personIds)
            {
                message.DirectPersons.Add(new CommunicationMessageRecipientPerson { PersonId = personId });
            }

            message.RecipientCount = await CountTargetPersonsAsync(groupIds, personIds, ct);
            message.RecipientSummary = await BuildRecipientSummaryAsync(groupIds, personIds, ct);

            await _context.SaveChangesAsync(ct);

            if (dto.Action == "send")
            {
                return await SendMessageInternalAsync(message, baseUrl, ct);
            }

            return new SaveMessageResponseDto { Success = true, MessageId = message.Id };
        }

        public async Task<SaveMessageResponseDto> SendExistingAsync(int id, string baseUrl, CancellationToken ct = default)
        {
            var message = await _context.CommunicationMessages
                .Include(m => m.Groups)
                .Include(m => m.DirectPersons)
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            if (message is null)
            {
                return new SaveMessageResponseDto { Success = false, ErrorMessage = "Beskeden blev ikke fundet." };
            }

            return await SendMessageInternalAsync(message, baseUrl, ct);
        }

        public async Task<bool> DeleteMessageAsync(int id, CancellationToken ct = default)
        {
            var message = await _context.CommunicationMessages.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (message is null)
            {
                return false;
            }

            // Groups/DirectPersons/Recipients cascade-delete with the message; the underlying
            // SmsMessage/CommunicationEmailMessage rows are kept — they're the actual delivery
            // record and aren't owned by this message.
            _context.CommunicationMessages.Remove(message);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        private async Task<SaveMessageResponseDto> SendMessageInternalAsync(CommunicationMessage message, string baseUrl, CancellationToken ct)
        {
            var groupIds = message.Groups.Select(g => g.PersonGroupId).ToList();
            var personIds = message.DirectPersons.Select(p => p.PersonId).ToList();

            var resolved = await ResolveRecipientsAsync(groupIds, personIds, message.ViaEmail, message.ViaSms, ct);
            if (resolved.Count == 0)
            {
                _logger.LogWarning("CommunicationMessage {Id} resolved to zero recipients", message.Id);
                return new SaveMessageResponseDto
                {
                    Success = false,
                    ErrorMessage = "Ingen modtagere med kontaktoplysninger for de valgte kanaler blev fundet.",
                    MessageId = message.Id
                };
            }

            Form? form = null;
            Arrangement? arrangement = null;
            var usePersonalFormLink = false;
            string? sharedFormLink = null;
            var needsPersonPublicIds = false;

            if (message.FormId.HasValue)
            {
                form = await GetLatestFormVersionAsync(message.FormId.Value, ct);
                if (form is not null)
                {
                    // Non-anonymous forms always require a personal link server-side, regardless of
                    // what the client sent — mirrors the compose modal's own JS rule.
                    usePersonalFormLink = !form.IsAnonymous || message.LinkType == "personal";
                    if (usePersonalFormLink)
                    {
                        needsPersonPublicIds = true;
                    }
                    else
                    {
                        sharedFormLink = $"{baseUrl}/Formular?Id={form.PublicId}";
                    }
                }
            }

            if (message.ArrangementId.HasValue)
            {
                arrangement = await _context.Arrangements.AsNoTracking().FirstOrDefaultAsync(a => a.Id == message.ArrangementId.Value, ct);
                if (arrangement is not null)
                {
                    // Tilmelding has no anonymous mode — the link is always personal.
                    needsPersonPublicIds = true;
                }
            }

            var personPublicIds = new Dictionary<int, Guid>();
            if (needsPersonPublicIds)
            {
                var distinctPersonIds = resolved.Select(r => r.PersonId).Distinct().ToList();
                personPublicIds = await _context.People
                    .Where(p => distinctPersonIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p.PublicId, ct);
            }

            foreach (var recipient in resolved)
            {
                var body = message.Body.Replace("{{Navn}}", recipient.DisplayName);

                if (form is not null)
                {
                    var link = usePersonalFormLink && personPublicIds.TryGetValue(recipient.PersonId, out var formPublicId)
                        ? $"{baseUrl}/Formular?Id={form.PublicId}&UId={formPublicId}"
                        : sharedFormLink;

                    if (link is not null)
                    {
                        body = $"{body}\n\n{form.Title}: {link}";
                    }
                }

                if (arrangement is not null && personPublicIds.TryGetValue(recipient.PersonId, out var arrangementPersonPublicId))
                {
                    var link = $"{baseUrl}/Tilmelding?Id={arrangement.PublicId}&UId={arrangementPersonPublicId}";
                    body = $"{body}\n\n{arrangement.Title}: {link}";
                }

                var recipientRow = new CommunicationMessageRecipient
                {
                    CommunicationMessageId = message.Id,
                    PersonId = recipient.PersonId,
                    DisplayName = recipient.DisplayName,
                    Channel = recipient.Channel,
                    Address = recipient.Address
                };

                if (recipient.Channel == CommunicationChannel.Sms)
                {
                    var sms = new SmsMessage
                    {
                        Direction = SmsDirection.Outbound,
                        PhoneNumber = recipient.Address,
                        Body = body,
                        Status = SmsMessageStatus.Pending
                    };
                    _context.SmsMessages.Add(sms);
                    recipientRow.SmsMessage = sms;
                }
                else
                {
                    var email = new CommunicationEmailMessage
                    {
                        ToAddress = recipient.Address,
                        Subject = message.Subject,
                        Body = body,
                        HtmlBody = BuildHtmlEmailBody(body),
                        Status = CommunicationEmailMessageStatus.Pending
                    };
                    _context.CommunicationEmailMessages.Add(email);
                    recipientRow.CommunicationEmailMessage = email;
                }

                _context.CommunicationMessageRecipients.Add(recipientRow);
            }

            message.Status = CommunicationMessageStatus.Sent;
            message.SentAtUtc = DateTime.UtcNow;
            message.UpdatedAtUtc = DateTime.UtcNow;
            message.RecipientCount = resolved.Select(r => r.PersonId).Distinct().Count();
            message.RecipientSummary = await BuildRecipientSummaryAsync(groupIds, personIds, ct);

            await _context.SaveChangesAsync(ct);

            return new SaveMessageResponseDto { Success = true, MessageId = message.Id };
        }

        private static CommunicationRecipientBadgesViewModel BuildRecipientBadges(CommunicationMessage message)
        {
            return new CommunicationRecipientBadgesViewModel
            {
                Groups = message.Groups
                    .Select(g => new PersonGroupOptionViewModel { Id = g.PersonGroupId, Name = g.PersonGroup.Name })
                    .OrderBy(g => g.Name)
                    .ToList(),
                DirectPersonNames = message.DirectPersons
                    .Select(dp => dp.Person.Name)
                    .OrderBy(n => n)
                    .ToList()
            };
        }

        /// <summary>
        /// Danish sentence telling the recipient upfront when they'll be able to pick shifts,
        /// so it can be dropped into the message body as soon as a Tilmelding link is picked.
        /// </summary>
        private static string BuildArrangementDefaultLinkText(ArrangementRegistrationStatus status, DateTime? opensAtUtc)
        {
            if (status == ArrangementRegistrationStatus.Open)
                return "Tilmeldingen er åben nu – I kan allerede gå ind og vælge vagter via linket.";

            var opensLocal = DateTime.SpecifyKind(opensAtUtc!.Value, DateTimeKind.Utc).ToLocalTime();
            return $"Tilmeldingen åbner {opensLocal.ToDanishDateTimeWithKl()} – I kan gå ind og vælge vagter fra det tidspunkt.";
        }

        /// <summary>
        /// Resolves a form id to the current latest version in its version series (Form.RootFormId
        /// chain), so a resend always uses whatever version is live now — with its own PublicId —
        /// even if the message was originally attached to a since-superseded version.
        /// </summary>
        private async Task<Form?> GetLatestFormVersionAsync(int formId, CancellationToken ct)
        {
            var attachedForm = await _context.Forms.AsNoTracking().FirstOrDefaultAsync(f => f.Id == formId, ct);
            if (attachedForm is null)
            {
                return null;
            }

            var rootId = attachedForm.RootFormId ?? attachedForm.Id;
            var seriesForms = await _context.Forms.AsNoTracking()
                .Where(f => f.Id == rootId || f.RootFormId == rootId)
                .ToListAsync(ct);

            return seriesForms.OrderByDescending(f => f.VersionNumber).FirstOrDefault();
        }

        private async Task<int> CountTargetPersonsAsync(IReadOnlyCollection<int> groupIds, IReadOnlyCollection<int> personIds, CancellationToken ct)
        {
            if (groupIds.Count == 0 && personIds.Count == 0)
            {
                return 0;
            }

            return await _context.People
                .Where(p => personIds.Contains(p.Id) || p.Memberships.Any(m => groupIds.Contains(m.GroupId)))
                .CountAsync(ct);
        }

        private async Task<string> BuildRecipientSummaryAsync(IReadOnlyCollection<int> groupIds, IReadOnlyCollection<int> personIds, CancellationToken ct)
        {
            var parts = new List<string>();

            if (groupIds.Count > 0)
            {
                var groupNames = await _context.PersonGroups
                    .Where(g => groupIds.Contains(g.Id))
                    .OrderBy(g => g.Name)
                    .Select(g => g.Name)
                    .ToListAsync(ct);
                parts.AddRange(groupNames);
            }

            if (personIds.Count > 0)
            {
                parts.Add(personIds.Count == 1 ? "1 person" : $"{personIds.Count} personer");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "Ingen modtagere";
        }

        /// <summary>
        /// Renders the plain-text email body (same text used for SMS) as HTML: bare http(s) links
        /// become clickable &lt;a href&gt; tags and line breaks become &lt;br&gt;. Sent as the HTML
        /// alternative alongside the plain-text Body — SmsMessage.Body is never touched by this,
        /// SMS always stays plain text.
        /// </summary>
        private static string BuildHtmlEmailBody(string plainBody)
        {
            var html = new StringBuilder();
            var lastIndex = 0;

            foreach (Match match in UrlPattern.Matches(plainBody))
            {
                html.Append(WebUtility.HtmlEncode(plainBody[lastIndex..match.Index]));
                var encodedUrl = WebUtility.HtmlEncode(match.Value);
                html.Append($"<a href=\"{encodedUrl}\">{encodedUrl}</a>");
                lastIndex = match.Index + match.Length;
            }

            html.Append(WebUtility.HtmlEncode(plainBody[lastIndex..]));

            var htmlBody = html.ToString().Replace("\r\n", "\n").Replace("\n", "<br>\n");
            return $"<!DOCTYPE html><html><body style=\"font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#1a1a1a;\">{htmlBody}</body></html>";
        }

        private static readonly Regex UrlPattern = new(@"https?://[^\s<>""]+", RegexOptions.Compiled);
    }
}
