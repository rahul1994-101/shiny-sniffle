namespace Infrastructure.Persistence.Entities;

/// <summary>
/// Chat thread agent selection. Stored on <see cref="ChatThread.ChatAgent"/> (not a lookup table).
/// <see cref="Assistant"/> is 0 — the default for new instances, <c>default(ChatAgent)</c>, and the DB column default.
/// </summary>
public enum ChatAgent
{
    /// <summary>Default assistant for new threads.</summary>
    Assistant = 0,

    Email = 1
}

/// <summary>Legacy provider preset on DTOs; endpoints come from <see cref="EmailProviderDefinition"/> catalog.</summary>
public enum EmailProvider
{
    Custom = 0,
    Gmail = 1
}

/// <summary>How a <see cref="Contact"/> row was created.</summary>
public enum ContactSource
{
    Manual = 0,
    Import = 1,
    FromEmail = 2
}
