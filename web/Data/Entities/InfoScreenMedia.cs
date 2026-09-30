namespace web.Data.Entities
{
    /// <summary>
    /// An image or video uploaded to an InfoScreen (its media library). The file lives under
    /// App_files/infoscreens/; slide elements refer to it by PublicId, which is also what the public
    /// /Infoskaerm/Media link uses, so the running screen can load it without login.
    /// </summary>
    public class InfoScreenMedia
    {
        public int Id { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public int InfoScreenId { get; set; }

        public int FileMetadataId { get; set; }

        /// <summary>"image" or "video" (InfoScreenRules.MediaKind*).</summary>
        public string Kind { get; set; } = "image";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual InfoScreen Screen { get; set; } = null!;

        public virtual FileMetadata FileMetadata { get; set; } = null!;
    }
}
