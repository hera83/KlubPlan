namespace web.Repositories.Registrations.Dtos
{
    public class SubmitArrangementAnswerDto
    {
        public int ArrangementFormFieldId { get; set; }
        public string? Value { get; set; }
        public List<string> Values { get; set; } = new();
    }
}
