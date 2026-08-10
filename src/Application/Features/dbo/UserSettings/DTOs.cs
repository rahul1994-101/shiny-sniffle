using Infrastructure.Persistence.dbo;

namespace Application.Features.dbo.UserSettings;

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
