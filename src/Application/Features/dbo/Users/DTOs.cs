namespace Application.Features.dbo.Users;

public class SessionDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;

    public static SessionDto FromEntity(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = $"{user.FirstName} {user.LastName}".Trim()
    };

    public T AsResponse<T>() where T : SessionDto, new() => new()
    {
        Id = Id,
        Email = Email,
        FullName = FullName
    };
}

public class GeneralSettingsDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public static GeneralSettingsDto FromEntity(User user) => new()
    {
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName
    };

    public T AsResponse<T>() where T : GeneralSettingsDto, new() => new()
    {
        Email = Email,
        FirstName = FirstName,
        LastName = LastName
    };
}
