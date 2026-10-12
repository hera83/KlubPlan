namespace web.Data.Models;

/// <summary>One designed label in a LabelCollection. Quantity is the number of copies on the printed sheets.</summary>
public class LabelCollectionItem : EntityBase
{
    public int LabelCollectionId { get; set; }

    public int Quantity { get; set; } = 1;

    /// <summary>Position in the collection — the print order.</summary>
    public int Order { get; set; }

    /// <summary>Background color (#rrggbb).</summary>
    public string Background { get; set; } = "#ffffff";

    /// <summary>
    /// The label's elements (text, løbenummer, image, QR code) with position and style, as JSON
    /// (Repositories/LabelCollections/Dtos/LabelElementDto). Validated and normalized on save; the
    /// designer draws it with wwwroot/js/label-design.js and the PDF with LabelDesignPdf.
    /// </summary>
    public string ElementsJson { get; set; } = "[]";

    public virtual LabelCollection Collection { get; set; } = null!;
}
