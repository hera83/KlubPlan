namespace web.Repositories.Forms.Dtos
{
    public class SubmitFormAnswerDto
    {
        public int FormFieldId { get; set; }
        public string? Value { get; set; }
        public List<string> Values { get; set; } = new();
    }

    public class SubmitFormRequestDto
    {
        public int FormId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public List<SubmitFormAnswerDto> Answers { get; set; } = new();
    }
}
