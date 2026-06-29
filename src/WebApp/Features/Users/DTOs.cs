namespace WebApp.Features.Users;

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
