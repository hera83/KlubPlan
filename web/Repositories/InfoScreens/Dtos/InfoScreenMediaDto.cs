namespace web.Repositories.InfoScreens.Dtos
{
    /// <summary>An image/video in a screen's media library (the designer's "Vælg billede/video").</summary>
    public class InfoScreenMediaDto
    {
        /// <summary>InfoScreenMedia.PublicId — what slide elements refer to.</summary>
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>image or video.</summary>
        public string Kind { get; set; } = "image";
        public long SizeBytes { get; set; }
    }
}
