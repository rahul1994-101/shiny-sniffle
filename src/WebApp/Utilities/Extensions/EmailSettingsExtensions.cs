using WebApp.Models;

namespace WebApp.Utilities.Extensions;

internal static class EmailSettingsExtensions
{
    internal static bool IsMailboxConfigured(this EmailSettings? settings) =>
        settings is not null &&
        !string.IsNullOrWhiteSpace(settings.EmailAddress) &&
        !string.IsNullOrWhiteSpace(settings.ImapHost) &&
        !string.IsNullOrWhiteSpace(settings.SmtpHost) &&
        !string.IsNullOrWhiteSpace(settings.Username) &&
        !string.IsNullOrWhiteSpace(settings.Password);

    /// <summary>Runtime copy with decrypted password for MailKit. Never persist the result.</summary>
    internal static EmailSettings? ToMailRuntime(this EmailSettings? settings)
    {
        if (settings is null || !settings.IsMailboxConfigured())
        {
            return null;
        }

        return new EmailSettings
        {
            Provider = settings.Provider,
            EmailAddress = settings.EmailAddress.Trim(),
            Username = settings.Username.Trim(),
            Password = settings.Password.Decrypt(),
            ImapHost = settings.ImapHost.Trim(),
            ImapPort = settings.ImapPort,
            ImapUseSsl = settings.ImapUseSsl,
            SmtpHost = settings.SmtpHost.Trim(),
            SmtpPort = settings.SmtpPort,
            SmtpUseSsl = settings.SmtpUseSsl
        };
    }
}
