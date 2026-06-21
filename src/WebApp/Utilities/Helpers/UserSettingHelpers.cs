using WebApp.Models;

namespace WebApp.Utilities.Helpers;

internal static class UserSettingHelpers
{
    internal static UserSettingsDto MapToDto(UserSetting? entity) =>
        new()
        {
            Email = EmailSettingsHelpers.MapToDto(entity?.EmailSettings)
        };
}
