using Infrastructure.Persistence.dbo;
using Infrastructure.Persistence.chat;
using Infrastructure.Persistence.workspace;

namespace Infrastructure.Persistence.Shared;

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

/// <summary>Legacy provider preset on DTOs; endpoints come from <see cref="EmailProvider"/> catalog table.</summary>
public enum EmailProviderPreset
{
    Custom = 0,
    Gmail = 1
}

/// <summary>How a <see cref="Contact"/> row was created. Set by the app, not the contact editor.</summary>
public enum ContactSource
{
    /// <summary>User created or edited in Workspace → Contacts.</summary>
    Manual = 0,

    /// <summary>Bulk or file import (future).</summary>
    Import = 1,

    /// <summary>Promoted from email/triage (e.g. “save as contact”) (future).</summary>
    FromEmail = 2,

    /// <summary>Created by the in-app assistant / agent tool (future).</summary>
    Agent = 3,

    /// <summary>External API or integration (future).</summary>
    Api = 4
}

/// <summary>Workspace referable object kind for tag/bucket membership (Contact = 0, Mailbox = 1).</summary>
public enum ReferableKind : byte
{
    Contact = 0,
    Mailbox = 1
}
