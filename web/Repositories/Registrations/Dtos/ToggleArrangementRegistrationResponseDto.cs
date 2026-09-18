namespace web.Repositories.Registrations.Dtos
{
    public class ToggleArrangementRegistrationResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>The RegistrationForcedOpen state right after the toggle.</summary>
        public bool IsOpen { get; set; }
    }
}
