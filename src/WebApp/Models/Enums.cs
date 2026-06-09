namespace WebApp.Models;

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

public enum ErrorCode
{
    BadRequest = 400,
    NotFound = 404,
    InternalServerError = 500,

    //Unauthorized = 401,
    //Forbidden = 403,

    UnknownError = 0
}
