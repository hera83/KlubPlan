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

            var query = _context.Forms.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var term = filter.SearchText.Trim();
                query = query.Where(f => f.Title.Contains(term));
            }

            var projected = query
                .OrderByDescending(f => f.CreatedAtUtc)
                .Select(f => new FormListItemViewModel
                {
                    Id = f.Id,
                    Title = f.Title,
                    Description = f.Description,
                    IsAcceptingResponses = f.IsAcceptingResponses,
                    CreatedAtUtc = f.CreatedAtUtc,
                    UpdatedAtUtc = f.UpdatedAtUtc,
                    FieldCount = f.Fields.Count,
                    ResponseCount = f.Submissions.Count,
                    HasCurrentUserSubmitted = f.Submissions.Any(s => s.SubmittedByUserId == userId)
                });

            filter.TotalCount = await query.CountAsync(ct);
            filter.Forms = await projected
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(ct);

            return filter;
        }

        public async Task<FormBuilderViewModel?> GetFormForBuilderAsync(int id, CancellationToken ct = default)
        {
            var form = await _context.Forms
                .AsNoTracking()
                .Include(f => f.Fields)
                .FirstOrDefaultAsync(f => f.Id == id, ct);

            if (form is null) return null;

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
                    .ToList()
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

        public async Task<bool> DeleteFormAsync(int id, CancellationToken ct = default)
        {
            var form = await _context.Forms.FirstOrDefaultAsync(f => f.Id == id, ct);
            if (form is null) return false;

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

            return new FormFillViewModel
            {
                FormId = form.Id,
                Title = form.Title,
                Description = form.Description,
                IsAcceptingResponses = form.IsAcceptingResponses,
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

            if (!form.IsAcceptingResponses)
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

            return new FormResponsesViewModel
            {
                FormId = form.Id,
                FormTitle = form.Title,
                Columns = form.Fields
                    .Where(f => FormFieldTypes.IsAnswerable(f.FieldType))
                    .OrderBy(f => f.Order)
                    .Select(f => new FormResponseColumnViewModel { FormFieldId = f.Id, Label = f.Label })
                    .ToList()
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
