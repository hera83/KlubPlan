using web.Constants;
using web.Repositories.Forms.Dtos;

namespace web.Managers.Forms.Dtos;

/// <summary>The public /Formular link's gate result, and the form when it exists.</summary>
public class PublicFormDto
{
    public PublicFormStatus Status { get; set; }
    public FormDto? Form { get; set; }
}
