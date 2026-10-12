using web.Constants;
using web.Managers.Arrangements.Dtos;
using web.Managers.Arrangements.Interfaces;
using web.Repositories.ArrangementRegistrations.Dtos;
using web.Repositories.ArrangementRegistrations.Interfaces;
using web.Repositories.Arrangements.Dtos;
using web.Repositories.Arrangements.Interfaces;
using web.Repositories.People.Interfaces;
using web.Repositories.PersonGroups.Interfaces;

namespace web.Managers.Arrangements;

public class ArrangementManager(
    IArrangementRepository arrangementRepository,
    IArrangementRegistrationRepository registrationRepository,
    IPersonRepository personRepository,
    IPersonGroupRepository personGroupRepository,
    ILogger<ArrangementManager> logger) : IArrangementManager
{
    private const int DefaultPageSize = 10;
    private const int MinPageSize = 5;
    private const int MaxPageSize = 500;

    public async Task<ArrangementBuilderDto?> GetBuilderAsync(int? id, CancellationToken cancellationToken = default)
    {
        ArrangementDto? arrangement = null;
        if (id is not null)
        {
            arrangement = await arrangementRepository.GetByIdAsync(id.Value, cancellationToken);
            if (arrangement is null)
                return null;
        }

        return new ArrangementBuilderDto
        {
            Arrangement = arrangement,
            Groups = await personGroupRepository.GetAllAsync(cancellationToken),
            People = await personRepository.GetOptionsAsync(cancellationToken)
        };
    }

    public async Task<PublicArrangementDto> GetPublicAsync(Guid arrangementPublicId, Guid? personPublicId, CancellationToken cancellationToken = default)
    {
        var (status, arrangement, personId) = await ResolvePublicAccessAsync(arrangementPublicId, personPublicId, cancellationToken);
        var result = new PublicArrangementDto { Status = status, Arrangement = arrangement };
        if (status != PublicArrangementStatus.Ok || arrangement is null)
            return result;

        result.PersonName = personId is null ? null : (await personRepository.GetByIdAsync(personId.Value, cancellationToken))?.Name;
        result.TakenCountByShift = await registrationRepository.GetTakenCountsAsync(arrangement.Shifts.Select(s => s.Id).ToList(), cancellationToken);
        return result;
    }

    public async Task<SubmitPublicArrangementResultDto> SubmitPublicAsync(Guid arrangementPublicId, Guid? personPublicId, SubmitPublicArrangementDto input, CancellationToken cancellationToken = default)
    {
        var (status, arrangement, personId) = await ResolvePublicAccessAsync(arrangementPublicId, personPublicId, cancellationToken);
        if (status != PublicArrangementStatus.Ok || arrangement is null || personId is null)
            return new SubmitPublicArrangementResultDto { Status = status };

        var (answers, fieldErrors) = ValidateAnswers(arrangement, input.Answers);
        if (fieldErrors.Count > 0)
            return Failed(fieldErrors: fieldErrors);

        var selectedShiftIds = input.SelectedShiftIds.Distinct().ToList();
        var shiftsById = arrangement.Shifts.ToDictionary(s => s.Id);

        if (selectedShiftIds.Any(id => !shiftsById.ContainsKey(id)))
            return Failed("En eller flere valgte vagter findes ikke længere.");
        if (selectedShiftIds.Count == 0 && arrangement.Shifts.Count > 0)
            return Failed("Vælg mindst én vagt.");

        // Capacity is re-checked here (not just client-side) — it can have changed since the form was shown.
        var takenCounts = await registrationRepository.GetTakenCountsAsync(selectedShiftIds, cancellationToken);
        var slots = new List<RegisteredShiftDto>();

        foreach (var shiftId in selectedShiftIds)
        {
            var shift = shiftsById[shiftId];

            // Named slots are only honored when the arrangement allows it — anything else posted is
            // ignored. Once the registrant has named anyone for a shift (the "+" flow), every named
            // slot — including the one pre-filled with their own name — replaces the implicit self
            // row, since they may have changed it to someone else (e.g. a parent taking the shift).
            var names = arrangement.AllowMultipleNamesPerShift
                ? input.Companions.Where(c => c.ShiftId == shiftId).Select(c => c.Name?.Trim()).ToList()
                : [];

            if (names.Any(string.IsNullOrWhiteSpace))
                return Failed($"Alle navne for '{shift.Title}' skal udfyldes.");

            var slotsRequested = names.Count > 0 ? names.Count : 1;
            var taken = takenCounts.GetValueOrDefault(shiftId);
            if (taken + slotsRequested > shift.NeededCount)
                return Failed($"'{shift.Title}' har kun {Math.Max(0, shift.NeededCount - taken)} ledig(e) plads(er) tilbage.");

            if (shift.Requirements.Count > 0 && shift.Requirements.Any(r => !input.ConfirmedRequirementIds.Contains(r.Id)))
                return Failed($"Du skal bekræfte alle krav for at tilmelde dig '{shift.Title}'.");

            if (names.Count > 0)
                slots.AddRange(names.Select(name => new RegisteredShiftDto { ArrangementShiftId = shiftId, CompanionName = name }));
            else
                slots.Add(new RegisteredShiftDto { ArrangementShiftId = shiftId });
        }

        await registrationRepository.CreateAsync(new CreateArrangementRegistrationDto
        {
            ArrangementId = arrangement.Id,
            PersonId = personId.Value,
            Answers = answers,
            Shifts = slots
        }, cancellationToken);

        logger.LogInformation("Person {PersonId} registered publicly for arrangement {ArrangementId}", personId, arrangement.Id);
        return new SubmitPublicArrangementResultDto { Status = PublicArrangementStatus.Ok, Success = true, ArrangementTitle = arrangement.Title };
    }

    public async Task<ArrangementRegistrationsDto?> GetRegistrationsAsync(int arrangementId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var registrations = await BuildRegistrationsShellAsync(arrangementId, cancellationToken);
        if (registrations is null)
            return null;

        registrations.Page = page < 1 ? 1 : page;
        registrations.PageSize = pageSize is < MinPageSize or > MaxPageSize ? DefaultPageSize : pageSize;
        registrations.TotalCount = await registrationRepository.CountAsync(arrangementId, cancellationToken);
        registrations.Rows = await registrationRepository.GetPageAsync(arrangementId, registrations.Page, registrations.PageSize, cancellationToken);
        return registrations;
    }

    public async Task<ArrangementRegistrationsDto?> GetAllRegistrationsAsync(int arrangementId, CancellationToken cancellationToken = default)
    {
        var registrations = await BuildRegistrationsShellAsync(arrangementId, cancellationToken);
        if (registrations is null)
            return null;

        registrations.TotalCount = await registrationRepository.CountAsync(arrangementId, cancellationToken);
        registrations.Page = 1;
        registrations.PageSize = registrations.TotalCount == 0 ? 1 : registrations.TotalCount;
        registrations.Rows = await registrationRepository.GetPageAsync(arrangementId, 1, registrations.PageSize, cancellationToken);
        return registrations;
    }

    private async Task<ArrangementRegistrationsDto?> BuildRegistrationsShellAsync(int arrangementId, CancellationToken cancellationToken)
    {
        var arrangement = await arrangementRepository.GetByIdAsync(arrangementId, cancellationToken);
        if (arrangement is null)
            return null;

        return new ArrangementRegistrationsDto
        {
            ArrangementId = arrangement.Id,
            ArrangementTitle = arrangement.Title,
            Columns = arrangement.FormFields
                .Where(f => FormFieldTypes.IsAnswerable(f.FieldType))
                .Select(f => new ArrangementRegistrationColumnDto { ArrangementFormFieldId = f.Id, Label = f.Label })
                .ToList()
        };
    }

    private async Task<(PublicArrangementStatus Status, ArrangementDto? Arrangement, int? PersonId)> ResolvePublicAccessAsync(Guid arrangementPublicId, Guid? personPublicId, CancellationToken cancellationToken)
    {
        var arrangement = await arrangementRepository.GetByPublicIdAsync(arrangementPublicId, cancellationToken);
        if (arrangement is null)
            return (PublicArrangementStatus.NotFound, null, null);

        var registrationStatus = ArrangementRegistrationStatuses.GetStatus(arrangement.RegistrationOpensAtUtc, arrangement.RegistrationClosesAtUtc, arrangement.RegistrationForcedOpen);
        if (registrationStatus == ArrangementRegistrationStatus.NotYetOpen)
            return (PublicArrangementStatus.NotYetOpen, arrangement, null);
        if (registrationStatus == ArrangementRegistrationStatus.Closed)
            return (PublicArrangementStatus.Closed, arrangement, null);

        if (personPublicId is null || personPublicId == Guid.Empty)
            return (PublicArrangementStatus.MissingIdentity, arrangement, null);

        var personId = await personRepository.GetIdByPublicIdAsync(personPublicId.Value, cancellationToken);
        if (personId is null)
            return (PublicArrangementStatus.InvalidIdentity, arrangement, null);

        if (arrangement.AccessMode == ArrangementAccessMode.Restricted)
        {
            var isDirectlyAllowed = arrangement.AllowedPersonIds.Contains(personId.Value);
            var isAllowedViaGroup = arrangement.AllowedGroupIds.Count > 0
                && await personRepository.IsInAnyGroupAsync(personId.Value, arrangement.AllowedGroupIds, cancellationToken);

            if (!isDirectlyAllowed && !isAllowedViaGroup)
                return (PublicArrangementStatus.NotAllowed, arrangement, personId);
        }

        return await registrationRepository.ExistsForPersonAsync(arrangement.Id, personId.Value, cancellationToken)
            ? (PublicArrangementStatus.AlreadyRegistered, arrangement, personId)
            : (PublicArrangementStatus.Ok, arrangement, personId);
    }

    /// <summary>Checks the posted answers against the answerable sign-up fields: required fields must be filled; checkbox values are joined with ", ".</summary>
    private static (List<ArrangementAnswerValueDto> Answers, Dictionary<int, string> FieldErrors) ValidateAnswers(ArrangementDto arrangement, List<ArrangementAnswerInputDto> answers)
    {
        var answersByField = answers.ToDictionary(a => a.ArrangementFormFieldId);
        var fieldErrors = new Dictionary<int, string>();
        var values = new List<ArrangementAnswerValueDto>();

        foreach (var field in arrangement.FormFields.Where(f => FormFieldTypes.IsAnswerable(f.FieldType)))
        {
            answersByField.TryGetValue(field.Id, out var answer);
            var isCheckboxes = FormFieldTypes.AllowsMultipleValues(field.FieldType);

            var multiple = isCheckboxes
                ? (answer?.Values ?? []).Where(v => !string.IsNullOrWhiteSpace(v)).ToList()
                : [];
            var single = isCheckboxes ? null : answer?.Value?.Trim();

            var isEmpty = isCheckboxes ? multiple.Count == 0 : string.IsNullOrWhiteSpace(single);
            if (field.IsRequired && isEmpty)
            {
                fieldErrors[field.Id] = "Dette felt er påkrævet.";
                continue;
            }

            values.Add(new ArrangementAnswerValueDto
            {
                ArrangementFormFieldId = field.Id,
                ValueText = isCheckboxes ? (multiple.Count > 0 ? string.Join(", ", multiple) : null) : single
            });
        }

        return (values, fieldErrors);
    }

    private static SubmitPublicArrangementResultDto Failed(string? shiftError = null, Dictionary<int, string>? fieldErrors = null) => new()
    {
        Status = PublicArrangementStatus.Ok,
        Success = false,
        ShiftError = shiftError,
        FieldErrors = fieldErrors ?? []
    };
}
