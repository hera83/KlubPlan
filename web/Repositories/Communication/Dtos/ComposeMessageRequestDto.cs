namespace web.Repositories.Communication.Dtos
{
    public class ComposeMessageRequestDto
    {
        /// <summary>0 = new message.</summary>
        public int Id { get; set; }

        public string Subject { get; set; } = string.Empty;

        public string? Body { get; set; }

        public bool ViaEmail { get; set; }

        public bool ViaSms { get; set; }

        public List<int> GroupIds { get; set; } = new();

        /// <summary>Kept as "AllowedPersonIds" to match _ArrangementPersonCheckboxes.cshtml's fixed checkbox name.</summary>
        public List<int> AllowedPersonIds { get; set; } = new();

        public int? FormId { get; set; }

        /// <summary>"shared" or "personal" — only meaningful when FormId is set.</summary>
        public string? LinkType { get; set; }

        /// <summary>Attached arrangement (Tilmelding), if any. Its link is always personal — Tilmelding has no anonymous mode.</summary>
        public int? ArrangementId { get; set; }

        /// <summary>"draft" or "send".</summary>
        public string Action { get; set; } = "draft";
    }
}
