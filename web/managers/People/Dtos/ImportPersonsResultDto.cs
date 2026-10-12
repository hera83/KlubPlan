namespace web.Managers.People.Dtos;

public class ImportPersonsResultDto
{
    public ImportPersonsStatus Status { get; set; }
    public int ImportedCount { get; set; }
    public List<ImportPersonErrorDto> Errors { get; set; } = [];
}
