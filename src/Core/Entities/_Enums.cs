namespace Core.Entities;

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

/// <summary>Mailbox provider preset for IMAP/SMTP endpoints stored on <see cref="EmailSettings"/>.</summary>
public enum EmailProvider
{
    Custom = 0,
    Gmail = 1
}
