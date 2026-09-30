namespace web.Repositories.LabelCollections.Dtos
{
    /// <summary>An image in a collection's image library (the designer's "Vælg billede").</summary>
    public class LabelMediaDto
    {
        /// <summary>LabelCollectionMedia.PublicId — what label elements refer to.</summary>
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }
}
