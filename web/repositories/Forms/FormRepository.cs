using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Models;
using web.Repositories.Forms.Dtos;
using web.Repositories.Forms.Interfaces;
using web.Repositories.Forms.Mappers;

namespace web.Repositories.Forms;

public class FormRepository(WebDbContext dbContext, ILogger<FormRepository> logger) : IFormRepository
{
    private const int DefaultPageSize = 10;
    private const int MinPageSize = 5;
    private const int MaxPageSize = 200;

    public async Task<FormPageDto> SearchAsync(SearchFormsDto search, string userId, CancellationToken cancellationToken = default)
    {
        var pageNumber = search.Page < 1 ? 1 : search.Page;
        var pageSize = search.PageSize is < MinPageSize or > MaxPageSize ? DefaultPageSize : search.PageSize;

        var formsQuery = dbContext.Forms.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            var term = search.SearchText.Trim();
            formsQuery = formsQuery.Where(f => f.Title.Contains(term));
        }

        // Only the latest version of each form series is shown in the table.
        var latestPerSeries = dbContext.Forms
            .GroupBy(f => f.RootFormId ?? f.Id)
            .Select(g => new { RootId = g.Key, MaxVersion = g.Max(f => f.VersionNumber), Count = g.Count() });

        var query =
            from f in formsQuery
            join lv in latestPerSeries
                on new { RootId = f.RootFormId ?? f.Id, Version = f.VersionNumber }
                equals new { RootId = lv.RootId, Version = lv.MaxVersion }
            select new { Form = f, lv.Count };

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(x => x.Form.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                Item = new FormListItemDto
                {
                    Id = x.Form.Id,
                    PublicId = x.Form.PublicId,
                    Title = x.Form.Title,
                    Description = x.Form.Description,
                    IsAcceptingResponses = x.Form.IsAcceptingResponses,
                    IsAnonymous = x.Form.IsAnonymous,
                    CreatedAt = x.Form.CreatedAt,
                    UpdatedAt = x.Form.UpdatedAt,
                    VersionNumber = x.Form.VersionNumber,
                    VersionCount = x.Count,
                    FieldCount = x.Form.Fields.Count,
                    ResponseCount = x.Form.Submissions.Count,
                    HasCurrentUserSubmitted = x.Form.Submissions.Any(s => s.SubmittedByUserId == userId)
                },
                RootId = x.Form.RootFormId ?? x.Form.Id
            })
            .ToListAsync(cancellationToken);

        var seriesNeedingVersions = rows.Where(r => r.Item.VersionCount > 1).Select(r => r.RootId).Distinct().ToList();
        if (seriesNeedingVersions.Count > 0)
        {
            var allVersions = await dbContext.Forms.AsNoTracking()
                .Where(f => seriesNeedingVersions.Contains(f.RootFormId ?? f.Id))
                .OrderByDescending(f => f.VersionNumber)
                .Select(f => new { f.Id, RootId = f.RootFormId ?? f.Id, f.VersionNumber, f.CreatedAt, f.IsAcceptingResponses })
                .ToListAsync(cancellationToken);

            foreach (var row in rows.Where(r => r.Item.VersionCount > 1))
            {
                row.Item.Versions = allVersions
                    .Where(v => v.RootId == row.RootId)
                    .Select(v => new FormVersionOptionDto
                    {
                        Id = v.Id,
                        VersionNumber = v.VersionNumber,
                        CreatedAt = v.CreatedAt,
                        IsAcceptingResponses = v.IsAcceptingResponses,
                        IsCurrent = v.Id == row.Item.Id
                    })
                    .ToList();
            }
        }

        return new FormPageDto { Items = rows.Select(r => r.Item).ToList(), TotalCount = totalCount, Page = pageNumber, PageSize = pageSize };
    }

    public async Task<FormDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        (await dbContext.Forms.AsNoTracking()
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken))?.ToDto();

    public async Task<FormDto?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        (await dbContext.Forms.AsNoTracking()
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.PublicId == publicId, cancellationToken))?.ToDto();

    public async Task<List<FormLinkOptionDto>> GetLatestVersionsAsync(bool acceptingResponsesOnly, CancellationToken cancellationToken = default)
    {
        var forms = await dbContext.Forms.AsNoTracking()
            .Select(f => new { f.Id, f.RootFormId, f.VersionNumber, f.Title, f.IsAnonymous, f.IsAcceptingResponses })
            .ToListAsync(cancellationToken);

        return forms
            .GroupBy(f => f.RootFormId ?? f.Id)
            .Select(g => g.OrderByDescending(f => f.VersionNumber).First())
            .Where(f => !acceptingResponsesOnly || f.IsAcceptingResponses)
            .OrderBy(f => f.Title)
            .Select(f => new FormLinkOptionDto { Id = f.Id, Title = f.Title, IsAnonymous = f.IsAnonymous })
            .ToList();
    }

    public async Task<FormLinkDto?> GetLatestLinkAsync(int id, CancellationToken cancellationToken = default)
    {
        var rootId = await dbContext.Forms.AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => (int?)(f.RootFormId ?? f.Id))
            .FirstOrDefaultAsync(cancellationToken);
        if (rootId is null)
            return null;

        return await dbContext.Forms.AsNoTracking()
            .Where(f => f.Id == rootId || f.RootFormId == rootId)
            .OrderByDescending(f => f.VersionNumber)
            .Select(f => new FormLinkDto { Id = f.Id, PublicId = f.PublicId, Title = f.Title, IsAnonymous = f.IsAnonymous })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FormVersionInfoDto?> GetVersionInfoAsync(int id, CancellationToken cancellationToken = default)
    {
        var form = await dbContext.Forms.AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new { f.Id, f.RootFormId, f.VersionNumber })
            .FirstOrDefaultAsync(cancellationToken);
        if (form is null)
            return null;

        var rootId = form.RootFormId ?? form.Id;
        var seriesVersions = await dbContext.Forms.AsNoTracking()
            .Where(f => (f.RootFormId ?? f.Id) == rootId)
            .OrderByDescending(f => f.VersionNumber)
            .Select(f => new { f.Id, f.VersionNumber })
            .ToListAsync(cancellationToken);

        var latest = seriesVersions[0];
        var isLatest = latest.Id == form.Id;
        var index = seriesVersions.FindIndex(v => v.Id == form.Id);

        return new FormVersionInfoDto
        {
            VersionNumber = form.VersionNumber,
            VersionCount = seriesVersions.Count,
            IsLatestVersion = isLatest,
            CurrentVersionId = isLatest ? null : latest.Id,
            PreviousVersionId = index + 1 < seriesVersions.Count ? seriesVersions[index + 1].Id : null,
            NextVersionId = index > 0 ? seriesVersions[index - 1].Id : null
        };
    }

    public async Task<bool> IsLatestVersionAsync(int id, CancellationToken cancellationToken = default)
    {
        var form = await dbContext.Forms.AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new { f.Id, f.RootFormId, f.VersionNumber })
            .FirstOrDefaultAsync(cancellationToken);
        if (form is null)
            return false;

        return form.VersionNumber == await LatestVersionNumberAsync(form.RootFormId ?? form.Id, cancellationToken);
    }

    public async Task<SaveFormResultDto> SaveAsync(SaveFormDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return Invalid("Formularen skal have en titel.");

        Form form;
        if (dto.Id > 0)
        {
            var existing = await dbContext.Forms
                .Include(f => f.Fields)
                .FirstOrDefaultAsync(f => f.Id == dto.Id, cancellationToken);
            if (existing is null)
                return new SaveFormResultDto { Status = SaveFormStatus.NotFound };
            if (!await IsLatestVersionAsync(existing.Id, cancellationToken))
                return new SaveFormResultDto { Status = SaveFormStatus.NotLatestVersion };

            form = existing;
            dbContext.Entry(form).State = EntityState.Modified;
        }
        else
        {
            form = dto.ToEntity();
            dbContext.Forms.Add(form);
        }

        dto.ApplyTo(form);

        var existingFields = form.Fields.ToDictionary(f => f.Id);
        var keptFieldIds = new HashSet<int>();

        foreach (var fieldDto in dto.Fields)
        {
            // Informationstekst (SectionHeading) er ren statisk tekst til respondenten og
            // skal ikke besvares — den må derfor godt stå uden overskrift/label.
            if (fieldDto.FieldType != FormFieldType.SectionHeading && string.IsNullOrWhiteSpace(fieldDto.Label))
                return Invalid("Alle spørgsmål skal have en label.");

            string? optionsJson = null;
            if (FormFieldTypes.HasOptions(fieldDto.FieldType))
            {
                var options = FormMapper.ParseOptions(fieldDto.OptionsJson);
                if (options.Count == 0)
                    return Invalid($"'{fieldDto.Label}' skal have mindst én mulighed.");

                optionsJson = JsonSerializer.Serialize(options);
            }

            if (fieldDto.Id > 0 && existingFields.TryGetValue(fieldDto.Id, out var field))
            {
                fieldDto.ApplyTo(field, optionsJson);
                keptFieldIds.Add(field.Id);
            }
            else
            {
                form.Fields.Add(fieldDto.ToEntity(optionsJson));
            }
        }

        // Fields that existed before but were not posted were removed in the builder — delete
        // them (cascades their collected answers).
        foreach (var removed in existingFields.Values.Where(f => !keptFieldIds.Contains(f.Id)))
            dbContext.FormFields.Remove(removed);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Form {FormId} saved with {FieldCount} fields", form.Id, dto.Fields.Count);
        return new SaveFormResultDto { Status = SaveFormStatus.Saved, FormId = form.Id };
    }

    public async Task<SaveFormResultDto> SetAcceptingResponsesAsync(int id, bool isAcceptingResponses, CancellationToken cancellationToken = default)
    {
        var form = await dbContext.Forms.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (form is null)
            return new SaveFormResultDto { Status = SaveFormStatus.NotFound };
        if (!await IsLatestVersionAsync(id, cancellationToken))
            return new SaveFormResultDto { Status = SaveFormStatus.NotLatestVersion };

        form.IsAcceptingResponses = isAcceptingResponses;
        dbContext.Entry(form).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SaveFormResultDto { Status = SaveFormStatus.Saved, FormId = id };
    }

    public async Task<CreateFormVersionResultDto> CreateNewVersionAsync(int sourceFormId, string? userId, CancellationToken cancellationToken = default)
    {
        var source = await dbContext.Forms
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.Id == sourceFormId, cancellationToken);
        if (source is null)
            return new CreateFormVersionResultDto { Status = CreateFormVersionStatus.NotFound };
        if (source.IsAcceptingResponses)
            return new CreateFormVersionResultDto { Status = CreateFormVersionStatus.StillAcceptingResponses };

        var rootId = source.RootFormId ?? source.Id;
        var latestVersionNumber = await LatestVersionNumberAsync(rootId, cancellationToken);
        if (source.VersionNumber != latestVersionNumber)
            return new CreateFormVersionResultDto { Status = CreateFormVersionStatus.NotLatestVersion };

        var newVersion = new Form
        {
            Title = source.Title,
            Description = source.Description,
            IsAcceptingResponses = true,
            IsAnonymous = source.IsAnonymous,
            RootFormId = rootId,
            VersionNumber = latestVersionNumber + 1,
            CreatedByUserId = userId
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

        dbContext.Forms.Add(newVersion);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Form {SourceFormId} versioned into new form {NewFormId} (v{VersionNumber})", sourceFormId, newVersion.Id, newVersion.VersionNumber);
        return new CreateFormVersionResultDto { Status = CreateFormVersionStatus.Created, FormId = newVersion.Id };
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var form = await dbContext.Forms.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (form is null)
            return false;

        // Deleting the root of a version series: promote the oldest remaining sibling to be the
        // new root first, so the series stays linked.
        if (form.RootFormId is null)
        {
            var siblings = await dbContext.Forms
                .Where(f => f.RootFormId == form.Id)
                .OrderBy(f => f.VersionNumber)
                .ToListAsync(cancellationToken);

            if (siblings.Count > 0)
            {
                var newRoot = siblings[0];
                newRoot.RootFormId = null;
                foreach (var sibling in siblings.Skip(1))
                    sibling.RootFormId = newRoot.Id;
            }
        }

        dbContext.Forms.Remove(form);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Form {FormId} deleted", id);
        return true;
    }

    private Task<int> LatestVersionNumberAsync(int rootId, CancellationToken cancellationToken) =>
        dbContext.Forms
            .Where(f => (f.RootFormId ?? f.Id) == rootId)
            .MaxAsync(f => f.VersionNumber, cancellationToken);

    private static SaveFormResultDto Invalid(string error) => new() { Status = SaveFormStatus.Invalid, ErrorMessage = error };
}
