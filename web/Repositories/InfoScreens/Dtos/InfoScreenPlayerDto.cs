namespace web.Repositories.InfoScreens.Dtos
{
    /// <summary>
    /// What the running screen (/Infoskaerm) loads and re-checks every InfoScreenRules.PollSeconds.
    /// Status: ok, inactive or notfound. Version changes whenever the screen is saved/(de)activated.
    /// </summary>
    public class InfoScreenPlayerDto
    {
        public string Status { get; set; } = "notfound";
        public string Title { get; set; } = string.Empty;
        public string AspectRatio { get; set; } = "16:9";
        public string Transition { get; set; } = "fade";
        public long Version { get; set; }

        /// <summary>The slides to play — hidden slides are left out.</summary>
        public List<InfoScreenSlideDto> Slides { get; set; } = new();

        public const string StatusOk = "ok";
        public const string StatusInactive = "inactive";
        public const string StatusNotFound = "notfound";
    }
}
