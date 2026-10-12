namespace web.Repositories.Users.Dtos;

public class UpdateUserResultDto
{
    public UpdateUserStatus Status { get; set; }

    /// <summary>Identity's own error descriptions when Status is Rejected.</summary>
    public string? ErrorMessage { get; set; }
}
