namespace web.Repositories.ActivityLists.Dtos
{
    public class ActivityListExportDto
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = "liste.xlsx";
    }
}
