namespace web.Repositories.Forms.Dtos
{
    public class SubmitPublicFormRequestDto
    {
        /// <summary>Form.PublicId from the Id query parameter.</summary>
        public Guid FormPublicId { get; set; }

        /// <summary>Person.PublicId from the UId query parameter; null when the form is anonymous.</summary>
        public Guid? PersonPublicId { get; set; }

        public List<SubmitFormAnswerDto> Answers { get; set; } = new();
    }
}
