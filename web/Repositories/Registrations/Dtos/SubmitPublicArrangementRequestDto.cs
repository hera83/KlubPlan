namespace web.Repositories.Registrations.Dtos
{
    public class SubmitPublicArrangementRequestDto
    {
        /// <summary>Arrangement.PublicId from the Id query parameter.</summary>
        public Guid ArrangementPublicId { get; set; }

        /// <summary>Person.PublicId from the UId query parameter — always required, Tilmelding has no anonymous mode.</summary>
        public Guid? PersonPublicId { get; set; }

        public List<SubmitArrangementAnswerDto> Answers { get; set; } = new();

        /// <summary>ArrangementShift ids the registrant selected.</summary>
        public List<int> SelectedShiftIds { get; set; } = new();

        /// <summary>ArrangementShiftRequirement ids the registrant ticked to self-declare they meet them.</summary>
        public List<int> ConfirmedRequirementIds { get; set; } = new();
    }
}
