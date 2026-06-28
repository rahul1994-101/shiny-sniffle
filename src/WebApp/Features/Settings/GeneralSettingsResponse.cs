namespace WebApp.Features.Settings;

public sealed class GeneralSettingsResponse
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    internal static GeneralSettingsResponse FromEntity(Core.Entities.User user) => new()
    {
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName
    };
}
