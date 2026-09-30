namespace web.Data.Entities
{
    /// <summary>
    /// An image uploaded to a LabelCollection (its image library). The file lives under App_files/labels/;
    /// label elements refer to it by PublicId.
    /// </summary>
    public class LabelCollectionMedia
    {
        public int Id { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public int LabelCollectionId { get; set; }

        public int FileMetadataId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual LabelCollection Collection { get; set; } = null!;

        public virtual FileMetadata FileMetadata { get; set; } = null!;
    }
}
