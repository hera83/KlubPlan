namespace web.ViewModels
{
    public class CommunicationIndexViewModel
    {
        public bool IsAdmin { get; set; }
        public List<CommunicationMessageListItemViewModel> Messages { get; set; } = new();
        public List<PersonGroupOptionViewModel> GroupOptions { get; set; } = new();
        public List<ArrangementPersonOptionViewModel> PersonOptions { get; set; } = new();
        public List<CommunicationFormOptionViewModel> FormOptions { get; set; } = new();
    }

    public class CommunicationMessageListItemViewModel
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public bool ViaEmail { get; set; }
        public bool ViaSms { get; set; }
        public string RecipientSummary { get; set; } = string.Empty;
        public int RecipientCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = "badge-info";
        public bool IsDraft { get; set; }
        public DateTime? SentAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public class CommunicationFormOptionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsAnonymous { get; set; }
    }

    public class CommunicationMessageDetailViewModel
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = "badge-info";
        public string RecipientSummary { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? SentAtUtc { get; set; }
        public List<CommunicationRecipientDetailViewModel> Recipients { get; set; } = new();
    }

    public class CommunicationRecipientDetailViewModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string DeliveryStatus { get; set; } = string.Empty;
    }
}
