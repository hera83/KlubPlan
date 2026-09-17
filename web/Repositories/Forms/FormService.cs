using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Repositories.Forms.Dtos;
using web.Repositories.Forms.Interfaces;
using web.ViewModels;

namespace web.Repositories.Forms
{
    public class FormService : IFormService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FormService> _logger;

        public FormService(ApplicationDbContext context, ILogger<FormService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<FormFilterViewModel> GetFormsAsync(FormFilterViewModel filter, string userId, bool isAdmin, CancellationToken ct = default)
        {
            filter.Page = filter.Page < 1 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize is < 5 or > 200 ? 10 : filter.PageSize;
            filter.IsAdmin = isAdmin;

            var formsQuery = _context.Forms.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var term = filter.SearchText.Trim();
                formsQuery = formsQuery.Where(f => f.Title.Contains(term));
            }

            // Only the latest version of each form series is shown in the table.
            var latestPerSeries = _context.Forms
                .GroupBy(f => f.RootFormId ?? f.Id)
                .Select(g => new { RootId = g.Key, MaxVersion = g.Max(f => f.VersionNumber), Count = g.Count() });

            var query =
                from f in formsQuery
                join lv in latestPerSeries
                    on new { RootId = f.RootFormId ?? f.Id, Version = f.VersionNumber }
                    equals new { RootId = lv.RootId, Version = lv.MaxVersion }
                select new { Form = f, lv.Count };

            filter.TotalCount = await query.CountAsync(ct);

            var page = await query
                .OrderByDescending(x => x.Form.CreatedAtUtc)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new
                {
                    x.Form.Id,
                    x.Form.Title,
                    x.Form.Description,
                    x.Form.IsAcceptingResponses,
                    x.Form.CreatedAtUtc,
                    x.Form.UpdatedAtUtc,
                    x.Form.VersionNumber,
                    x.Form.RootFormId,
                    VersionCount = x.Count,
                    FieldCount = x.Form.Fields.Count,
                    ResponseCount = x.Form.Submissions.Count,
                    HasCurrentUserSubmitted = x.Form.Submissions.Any(s => s.SubmittedByUserId == userId)
                })
                .ToListAsync(ct);

            filter.Forms = page.Select(p => new FormListItemViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                IsAcceptingResponses = p.IsAcceptingResponses,
                CreatedAtUtc = p.CreatedAtUtc,
                UpdatedAtUtc = p.UpdatedAtUtc,
                FieldCount = p.FieldCount,
                ResponseCount = p.ResponseCount,
                HasCurrentUserSubmitted = p.HasCurrentUserSubmitted,
                VersionNumber = p.VersionNumber,
                VersionCount = p.VersionCount
            }).ToList();

            var seriesNeedingVersions = page
                .Where(p => p.VersionCount > 1)
                .Select(p => p.RootFormId ?? p.Id)
                .Distinct()
                .ToList();

            if (seriesNeedingVersions.Count > 0)
            {
                var allVersions = await _context.Forms
                    .Where(f => seriesNeedingVersions.Contains(f.RootFormId ?? f.Id))
                    .OrderByDescending(f => f.VersionNumber)
                    .Select(f => new { f.Id, RootId = f.RootFormId ?? f.Id, f.VersionNumber, f.CreatedAtUtc, f.IsAcceptingResponses })
                    .ToListAsync(ct);

                var rootByFormId = page.ToDictionary(p => p.Id, p => p.RootFormId ?? p.Id);

                foreach (var item in filter.Forms)
                {
                    if (item.VersionCount <= 1)
                        continue;

                    var rootId = rootByFormId[item.Id];
                    item.Versions = allVersions
                        .Where(v => v.RootId == rootId)
                        .Select(v => new FormVersionOptionViewModel
                        {
                            Id = v.Id,
                            VersionNumber = v.VersionNumber,
                            CreatedAtUtc = v.CreatedAtUtc,
                            IsAcceptingResponses = v.IsAcceptingResponses,
                            IsCurrent = v.Id == item.Id
                        })
                        .ToList();
                }
            }

            return filter;
        }

        public async Task<FormBuilderViewModel?> GetFormForBuilderAsync(int id, CancellationToken ct = default)
        {
            var form = await _context.Forms
                .AsNoTracking()
                .Include(f => f.Fields)
                .FirstOrDefaultAsync(f => f.Id == id, ct);

            if (form is null) return null;

            var versionNav = await GetVersionNavAsync(form.Id, form.RootFormId, form.VersionNumber, ct);

            return new FormBuilderViewModel
            {
                Id = form.Id,
                Title = form.Title,
                Description = form.Description,
                IsAcceptingResponses = form.IsAcceptingResponses,
                Fields = form.Fields
                    .OrderBy(fl => fl.Order)
                    .Select(fl => new FormFieldBuilderViewModel
                    {
                        Id = fl.Id,
                        Label = fl.Label,
                        HelpText = fl.HelpText,
                        FieldType = fl.FieldType,
                        IsRequired = fl.IsRequired,
                        Order = fl.Order,
                        OptionsJson = fl.OptionsJson
                    })
                    .ToList(),
                VersionNumber = versionNav.VersionNumber,
                VersionCount = versionNav.VersionCount,
                IsLatestVersion = versionNav.IsLatestVersion,
                CurrentVersionId = versionNav.CurrentVersionId,
                PreviousVersionId = versionNav.PreviousVersionId,
                NextVersionId = versionNav.NextVersionId
            };
        }

        public async Task<SaveFormResponseDto> SaveFormAsync(SaveFormRequestDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return new SaveFormResponseDto { Success = false, ErrorMessage = "Formularen skal have en titel." };

            Form form;
            if (dto.Id > 0)
            {
                var existing = await _context.Forms
                    .Include(f => f.Fields)
                    .FirstOrDefaultAsync(f => f.Id == dto.Id, ct);

                if (existing is null)
                    return new SaveFormResponseDto { Success = false, ErrorMessage = "Formularen blev ikke fundet." };

                if (!await IsLatestVersionAsync(existing.Id, ct))
                    return new SaveFormResponseDto { Success = false, ErrorMessage = "Der findes en nyere version af denne formular — kun den aktuelle version kan redigeres." };

                form = existing;
                form.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                form = new Form
                {
                    CreatedByUserId = dto.UserId,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _context.Forms.Add(form);
            }

            form.Title = dto.Title.Trim();
            form.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            form.IsAcceptingResponses = dto.IsAcceptingResponses;

            var existingFields = form.Fields.ToDictionary(f => f.Id);
            var keptFieldIds = new HashSet<int>();

            foreach (var fieldDto in dto.Fields)
            {
                // Informationstekst (SectionHeading) er ren statisk tekst til respondenten og
                // skal ikke besvares — den må derfor godt stå uden overskrift/label.
                if (fieldDto.FieldType != FormFieldType.SectionHeading && string.IsNullOrWhiteSpace(fieldDto.Label))
                    return new SaveFormResponseDto { Success = false, ErrorMessage = "Alle spørgsmål skal have en label." };

                string? optionsJson = null;
                if (FormFieldTypes.HasOptions(fieldDto.FieldType))
                {
                    var options = ParseOptions(fieldDto.OptionsJson);
                    if (options.Count == 0)
                        return new SaveFormResponseDto { Success = false, ErrorMessage = $"'{fieldDto.Label}' skal have mindst én mulighed." };

                    optionsJson = JsonSerializer.Serialize(options);
                }

                if (fieldDto.Id > 0 && existingFields.TryGetValue(fieldDto.Id, out var field))
                {
                    field.Label = fieldDto.Label.Trim();
                    field.HelpText = string.IsNullOrWhiteSpace(fieldDto.HelpText) ? null : fieldDto.HelpText.Trim();
                    field.FieldType = fieldDto.FieldType;
                    field.IsRequired = fieldDto.IsRequired;
                    field.Order = fieldDto.Order;
                    field.OptionsJson = optionsJson;
                    keptFieldIds.Add(field.Id);
                }
                else
                {
                    var newField = new FormField
                    {
                        Label = fieldDto.Label.Trim(),
                        HelpText = string.IsNullOrWhiteSpace(fieldDto.HelpText) ? null : fieldDto.HelpText.Trim(),
                        FieldType = fieldDto.FieldType,
                        IsRequired = fieldDto.IsRequired,
                        Order = fieldDto.Order,
                        OptionsJson = optionsJson
                    };
                    form.Fields.Add(newField);
                }
            }

            // Fields that existed before but were not present in the posted list were removed
            // in the builder — delete them (cascades their collected FormAnswers).
            foreach (var removed in existingFields.Values.Where(f => !keptFieldIds.Contains(f.Id)))
            {
                _context.FormFields.Remove(removed);
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Form {FormId} saved with {FieldCount} fields", form.Id, dto.Fields.Count);
            return new SaveFormResponseDto { Success = true, FormId = form.Id };
        }

        public async Task<SaveFormResponseDto> CreateNewVersionAsync(int sourceFormId, string? userId, CancellationToken ct = default)
        {
            var source = await _context.Forms
                .Include(f => f.Fields)
                .FirstOrDefaultAsync(f => f.Id == sourceFormId, ct);

            if (source is null)
                return new SaveFormResponseDto { Success = false, ErrorMessage = "Formularen blev ikke fundet." };

            if (source.IsAcceptingResponses)
                return new SaveFormResponseDto { Success = false, ErrorMessage = "Der kan kun oprettes en ny version, når formularen er lukket for svar." };

            var effectiveRootId = source.RootFormId ?? source.Id;
            var latestVersionNumber = await _context.Forms
                .Where(f => (f.RootFormId ?? f.Id) == effectiveRootId)
                .MaxAsync(f => f.VersionNumber, ct);

            if (source.VersionNumber != latestVersionNumber)
                return new SaveFormResponseDto { Success = false, ErrorMessage = "Der findes allerede en nyere version af denne formular." };

            var newVersion = new Form
            {
                Title = source.Title,
                Description = source.Description,
                IsAcceptingResponses = true,
                RootFormId = effectiveRootId,
                VersionNumber = latestVersionNumber + 1,
                CreatedByUserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };

            foreach (var field in source.Fields.OrderBy(f => f.Order))
            {
                newVersion.Fields.Add(new FormField
                {
                    Label = field.Label,
                    HelpText = field.HelpText,
                    FieldType = field.FieldType,
                    IsRequired = field.IsRequired,
                    Order = field.Order,
                    OptionsJson = field.OptionsJson
                });
            }

            _context.Forms.Add(newVersion);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Form {SourceFormId} versioned into new form {NewFormId} (v{VersionNumber})", sourceFormId, newVersion.Id, newVersion.VersionNumber);
            return new SaveFormResponseDto { Success = true, FormId = newVersion.Id };
        }

        public async Task<bool> DeleteFormAsync(int id, CancellationToken ct = default)
        {
            var form = await _context.Forms.FirstOrDefaultAsync(f => f.Id == id, ct);
            if (form is null) return false;

            // If we're deleting the root of a version series, promote the oldest remaining
            // sibling to be the new root before removing this one, so the series stays linked.
            if (form.RootFormId is null)
            {
                var siblings = await _context.Forms
                    .Where(f => f.RootFormId == form.Id)
                    .OrderBy(f => f.VersionNumber)
                    .ToListAsync(ct);

                if (siblings.Count > 0)
                {
                    var newRoot = siblings[0];
                    newRoot.RootFormId = null;
                    foreach (var sibling in siblings.Skip(1))
                    {
                        sibling.RootFormId = newRoot.Id;
                    }
                }
            }

            _context.Forms.Remove(form);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Form {FormId} deleted", id);
            return true;
        }

        public async Task<FormFillViewModel?> GetFormForFillAsync(int id, CancellationToken ct = default)
        {
            var form = await _context.Forms
                .AsNoTracking()
                .Include(f => f.Fields)
                .FirstOrDefaultAsync(f => f.Id == id, ct);

            if (form is null) return null;

            var isLatestVersion = await IsLatestVersionAsync(form.Id, ct);

            return new FormFillViewModel
            {
                FormId = form.Id,
                Title = form.Title,
                Description = form.Description,
                IsAcceptingResponses = form.IsAcceptingResponses && isLatestVersion,
                Fields = form.Fields
                    .OrderBy(f => f.Order)
                    .Select(f => new FormFieldFillViewModel
                    {
                        FormFieldId = f.Id,
                        Label = f.Label,
                        HelpText = f.HelpText,
                        FieldType = f.FieldType,
                        IsRequired = f.IsRequired,
                        Options = ParseOptions(f.OptionsJson)
                    })
                    .ToList()
            };
        }

        public async Task<SubmitFormResponseDto> SubmitFormAsync(SubmitFormRequestDto dto, CancellationToken ct = default)
        {
            var form = await _context.Forms
                .Include(f => f.Fields)
                .FirstOrDefaultAsync(f => f.Id == dto.FormId, ct);

            if (form is null)
                return new SubmitFormResponseDto { Success = false, ErrorMessage = "Formularen blev ikke fundet." };

            if (!form.IsAcceptingResponses || !await IsLatestVersionAsync(form.Id, ct))
                return new SubmitFormResponseDto { Success = false, ErrorMessage = "Formularen tager ikke længere imod svar." };

            var answersByField = dto.Answers.ToDictionary(a => a.FormFieldId);
            var fieldErrors = new Dictionary<int, string>();
            var submission = new FormSubmission
            {
                FormId = form.Id,
                SubmittedByUserId = dto.UserId,
                SubmittedAtUtc = DateTime.UtcNow
            };

            foreach (var field in form.Fields.Where(f => FormFieldTypes.IsAnswerable(f.FieldType)).OrderBy(f => f.Order))
            {
                answersByField.TryGetValue(field.Id, out var answer);
                var isCheckboxes = FormFieldTypes.AllowsMultipleValues(field.FieldType);

                var values = isCheckboxes
                    ? (answer?.Values ?? new List<string>()).Where(v => !string.IsNullOrWhiteSpace(v)).ToList()
                    : new List<string>();
                var singleValue = isCheckboxes ? null : answer?.Value?.Trim();

                var isEmpty = isCheckboxes ? values.Count == 0 : string.IsNullOrWhiteSpace(singleValue);

                if (field.IsRequired && isEmpty)
                {
                    fieldErrors[field.Id] = "Dette felt er påkrævet.";
                    continue;
                }

                var valueText = isCheckboxes ? (values.Count > 0 ? string.Join(", ", values) : null) : singleValue;
                submission.Answers.Add(new FormAnswer { FormFieldId = field.Id, ValueText = valueText });
            }

            if (fieldErrors.Count > 0)
                return new SubmitFormResponseDto { Success = false, FieldErrors = fieldErrors };

            _context.FormSubmissions.Add(submission);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("User {UserId} submitted a response to form {FormId}", dto.UserId, dto.FormId);
            return new SubmitFormResponseDto { Success = true };
        }

        public async Task<FormResponsesViewModel?> GetResponsesAsync(int formId, int page, int pageSize, CancellationToken ct = default)
        {
            var vm = await BuildResponsesShellAsync(formId, ct);
            if (vm is null) return null;

            vm.Page = page < 1 ? 1 : page;
            vm.PageSize = pageSize is < 5 or > 500 ? 10 : pageSize;

            vm.TotalCount = await _context.FormSubmissions.CountAsync(s => s.FormId == formId, ct);
            vm.Rows = await BuildRowsAsync(formId, vm.Page, vm.PageSize, ct);

            return vm;
        }

        public async Task<FormResponsesViewModel?> GetAllResponsesForExportAsync(int formId, CancellationToken ct = default)
        {
            var vm = await BuildResponsesShellAsync(formId, ct);
            if (vm is null) return null;

            vm.TotalCount = await _context.FormSubmissions.CountAsync(s => s.FormId == formId, ct);
            vm.Page = 1;
            vm.PageSize = vm.TotalCount == 0 ? 1 : vm.TotalCount;
            vm.Rows = await BuildRowsAsync(formId, 1, vm.PageSize, ct);

            return vm;
        }

        private async Task<FormResponsesViewModel?> BuildResponsesShellAsync(int formId, CancellationToken ct)
        {
            var form = await _context.Forms
                .AsNoTracking()
                .Include(f => f.Fields)
                .FirstOrDefaultAsync(f => f.Id == formId, ct);

            if (form is null) return null;

            var versionNav = await GetVersionNavAsync(form.Id, form.RootFormId, form.VersionNumber, ct);

            return new FormResponsesViewModel
            {
                FormId = form.Id,
                FormTitle = form.Title,
                Columns = form.Fields
                    .Where(f => FormFieldTypes.IsAnswerable(f.FieldType))
                    .OrderBy(f => f.Order)
                    .Select(f => new FormResponseColumnViewModel { FormFieldId = f.Id, Label = f.Label })
                    .ToList(),
                VersionNumber = versionNav.VersionNumber,
                VersionCount = versionNav.VersionCount,
                IsLatestVersion = versionNav.IsLatestVersion,
                CurrentVersionId = versionNav.CurrentVersionId,
                PreviousVersionId = versionNav.PreviousVersionId,
                NextVersionId = versionNav.NextVersionId
            };
        }

        private async Task<List<FormResponseRowViewModel>> BuildRowsAsync(int formId, int page, int pageSize, CancellationToken ct)
        {
            var submissions = await _context.FormSubmissions
                .AsNoTracking()
                .Where(s => s.FormId == formId)
                .OrderByDescending(s => s.SubmittedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(s => s.Answers)
                .ToListAsync(ct);

            if (submissions.Count == 0) return new List<FormResponseRowViewModel>();

            var userIds = submissions.Select(s => s.SubmittedByUserId).Distinct().ToList();
            var displayNames = await _context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

            return submissions.Select(s => new FormResponseRowViewModel
            {
                SubmissionId = s.Id,
                SubmittedByDisplayName = displayNames.TryGetValue(s.SubmittedByUserId, out var name) ? name : "Ukendt bruger",
                SubmittedAtUtc = s.SubmittedAtUtc,
                Answers = s.Answers.ToDictionary(a => a.FormFieldId, a => a.ValueText ?? string.Empty)
            }).ToList();
        }

        private async Task<bool> IsLatestVersionAsync(int formId, CancellationToken ct)
        {
            var form = await _context.Forms.AsNoTracking()
                .Select(f => new { f.Id, f.RootFormId, f.VersionNumber })
                .FirstOrDefaultAsync(f => f.Id == formId, ct);

            if (form is null) return false;

            var effectiveRootId = form.RootFormId ?? form.Id;
            var latestVersionNumber = await _context.Forms
                .Where(f => (f.RootFormId ?? f.Id) == effectiveRootId)
                .MaxAsync(f => f.VersionNumber, ct);

            return form.VersionNumber == latestVersionNumber;
        }

        private record VersionNav(int VersionNumber, int VersionCount, bool IsLatestVersion, int? CurrentVersionId, int? PreviousVersionId, int? NextVersionId);

        private async Task<VersionNav> GetVersionNavAsync(int formId, int? rootFormId, int versionNumber, CancellationToken ct)
        {
            var effectiveRootId = rootFormId ?? formId;
            var seriesVersions = await _context.Forms
                .Where(f => (f.RootFormId ?? f.Id) == effectiveRootId)
                .OrderByDescending(f => f.VersionNumber)
                .Select(f => new { f.Id, f.VersionNumber })
                .ToListAsync(ct);

            var latestInSeries = seriesVersions[0];
            var isLatestVersion = latestInSeries.Id == formId;
            var currentIndex = seriesVersions.FindIndex(v => v.Id == formId);

            return new VersionNav(
                VersionNumber: versionNumber,
                VersionCount: seriesVersions.Count,
                IsLatestVersion: isLatestVersion,
                CurrentVersionId: isLatestVersion ? null : latestInSeries.Id,
                PreviousVersionId: currentIndex + 1 < seriesVersions.Count ? seriesVersions[currentIndex + 1].Id : null,
                NextVersionId: currentIndex > 0 ? seriesVersions[currentIndex - 1].Id : null);
        }

        private static List<string> ParseOptions(string? optionsJson)
        {
            if (string.IsNullOrWhiteSpace(optionsJson)) return new List<string>();

            try
            {
                var options = JsonSerializer.Deserialize<List<string>>(optionsJson) ?? new List<string>();
                return options
                    .Select(o => o?.Trim() ?? string.Empty)
                    .Where(o => !string.IsNullOrWhiteSpace(o))
                    .ToList();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }
    }
}
