namespace web.Repositories.LabelCollections.Dtos;

public enum SaveLabelCollectionStatus
{
    Saved,
    NotFound,

    /// <summary>The input broke a rule (LabelRules/LabelDesignRules). ErrorMessage says which.</summary>
    Invalid
}
