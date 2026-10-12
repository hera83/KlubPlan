namespace web.Repositories.InfoScreens.Dtos;

public enum SaveInfoScreenStatus
{
    Saved,
    NotFound,

    /// <summary>The input broke a rule (InfoScreenRules). ErrorMessage says which.</summary>
    Invalid
}
