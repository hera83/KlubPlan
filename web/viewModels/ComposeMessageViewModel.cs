using Microsoft.AspNetCore.Http;

namespace web.ViewModels;

/// <summary>The shared compose modal's form (Kommunikation and the activity page).</summary>
public class ComposeMessageViewModel
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

    /// <summary>Set when composing from an Activity's detail page — the message is then logged against that activity and the Save action redirects back there instead of Communication/Index.</summary>
    public int? ActivityId { get; set; }

    /// <summary>"draft" or "send".</summary>
    public string Action { get; set; } = "draft";

    /// <summary>Files to attach when sending via e-mail. Ignored (never persisted) unless ViaEmail is true — SMS has no concept of attachments.</summary>
    public List<IFormFile>? Attachments { get; set; }
}
