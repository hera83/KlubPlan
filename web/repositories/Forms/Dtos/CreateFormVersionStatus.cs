namespace web.Repositories.Forms.Dtos;

public enum CreateFormVersionStatus
{
    Created,
    NotFound,

    /// <summary>The form must be closed for responses first.</summary>
    StillAcceptingResponses,

    /// <summary>A newer version already exists.</summary>
    NotLatestVersion
}
