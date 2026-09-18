using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
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
                .OrderBy(f => f.Title)
                .Select(f => new CommunicationFormOptionViewModel { Id = f.Id, Title = f.Title, IsAnonymous = f.IsAnonymous })
                .ToList();

            var messageEntities = await _context.CommunicationMessages
                .AsNoTracking()
                .OrderByDescending(m => m.CreatedAtUtc)
                .ToListAsync(ct);

            var messages = messageEntities.Select(m => new CommunicationMessageListItemViewModel
            {
                Id = m.Id,
                Subject = m.Subject,
                ViaEmail = m.ViaEmail,
                ViaSms = m.ViaSms,
                RecipientSummary = m.RecipientSummary,
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
                FormOptions = formOptions
            };
        }

        public async Task<CommunicationMessageDetailViewModel?> GetDetailsAsync(int id, CancellationToken ct = default)
        {
            var message = await _context.CommunicationMessages
                .AsNoTracking()
                .Include(m => m.Recipients).ThenInclude(r => r.SmsMessage)
                .Include(m => m.Recipients).ThenInclude(r => r.CommunicationEmailMessage)
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            if (message is null)
            {
                return null;
            }

            return new CommunicationMessageDetailViewModel
            {
                Id = message.Id,
                Subject = message.Subject,
                Body = message.Body,
                Status = CommunicationMessageStatuses.GetUILabel(message.Status),
                StatusBadgeClass = CommunicationMessageStatuses.GetBadgeClass(message.Status),
                RecipientSummary = message.RecipientSummary,
                CreatedAtUtc = message.CreatedAtUtc,
                SentAtUtc = message.SentAtUtc,
                Recipients = message.Recipients
                    .OrderBy(r => r.DisplayName)
                    .Select(r => new CommunicationRecipientDetailViewModel
                    {
                        DisplayName = r.DisplayName,
                        Channel = r.Channel,
                        Address = r.Address,
                        DeliveryStatus = r.Channel == CommunicationChannel.Sms
                            ? (r.SmsMessage?.Status ?? SmsMessageStatus.Pending)
                            : (r.CommunicationEmailMessage?.Status.ToString() ?? CommunicationEmailMessageStatus.Pending.ToString())
                    })
                    .ToList()
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
            var usePersonalLink = false;
            string? sharedLink = null;
            var personPublicIds = new Dictionary<int, Guid>();

            if (message.FormId.HasValue)
            {
                form = await _context.Forms.AsNoTracking().FirstOrDefaultAsync(f => f.Id == message.FormId.Value, ct);
                if (form is not null)
                {
                    // Non-anonymous forms always require a personal link server-side, regardless of
                    // what the client sent — mirrors the compose modal's own JS rule.
                    usePersonalLink = !form.IsAnonymous || message.LinkType == "personal";
                    if (usePersonalLink)
                    {
                        var distinctPersonIds = resolved.Select(r => r.PersonId).Distinct().ToList();
                        personPublicIds = await _context.People
                            .Where(p => distinctPersonIds.Contains(p.Id))
                            .ToDictionaryAsync(p => p.Id, p => p.PublicId, ct);
                    }
                    else
                    {
                        sharedLink = $"{baseUrl}/Formular?Id={form.PublicId}";
                    }
                }
            }

            foreach (var recipient in resolved)
            {
                var body = message.Body.Replace("{{Navn}}", recipient.DisplayName);

                if (form is not null)
                {
                    var link = usePersonalLink && personPublicIds.TryGetValue(recipient.PersonId, out var publicId)
                        ? $"{baseUrl}/Formular?Id={form.PublicId}&UId={publicId}"
                        : sharedLink;

                    if (link is not null)
                    {
                        body = $"{body}\n\n{form.Title}: {link}";
                    }
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
    }
}
