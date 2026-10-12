namespace web.Repositories.Users.Dtos;

public enum UpdateUserStatus
{
    Updated,
    NotFound,
    UserNameTaken,

    /// <summary>Identity rejected the change (password rules, invalid user name …). See ErrorMessage.</summary>
    Rejected
}
