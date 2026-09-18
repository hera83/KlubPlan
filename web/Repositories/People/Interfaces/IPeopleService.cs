using web.Repositories.People.Dtos;
using web.ViewModels;

namespace web.Repositories.People.Interfaces
{
    public interface IPeopleService
    {
        Task<PersonFilterViewModel> GetPeopleAsync(PersonFilterViewModel filter, CancellationToken ct = default);

        Task<PersonDetailViewModel?> GetPersonDetailAsync(int id, CancellationToken ct = default);

        Task<CreatePersonResponseDto> CreatePersonAsync(CreatePersonRequestDto dto, CancellationToken ct = default);

        Task<UpdatePersonResponseDto> UpdatePersonAsync(UpdatePersonRequestDto dto, CancellationToken ct = default);

        Task<bool> DeletePersonAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Swaps Person.PublicId for a fresh Guid, invalidating any public /Formular link that
        /// used the old value. Nothing else in the system references PublicId (all internal
        /// relations use the int Id), so this is always safe to do — e.g. if a link leaked.
        /// </summary>
        Task<bool> RegeneratePublicIdAsync(int id, CancellationToken ct = default);

        Task<List<PersonGroupViewModel>> GetGroupsAsync(CancellationToken ct = default);

        Task<PersonGroupMutationResponseDto> CreateGroupAsync(string name, CancellationToken ct = default);

        Task<PersonGroupMutationResponseDto> UpdateGroupAsync(int id, string name, CancellationToken ct = default);

        Task<PersonGroupMutationResponseDto> DeleteGroupAsync(int id, CancellationToken ct = default);

        Task<ImportPersonsToGroupResponseDto> ImportPersonsToGroupAsync(int groupId, List<string> rawNames, CancellationToken ct = default);
    }
}
