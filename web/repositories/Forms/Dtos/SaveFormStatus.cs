namespace web.Repositories.Forms.Dtos;

public enum SaveFormStatus
{
    Saved,
    NotFound,

    /// <summary>A newer version exists — only the latest version can be changed.</summary>
    NotLatestVersion,

    /// <summary>A rule was broken (title, labels, options). ErrorMessage says which.</summary>
    Invalid
}
