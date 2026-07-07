namespace Application.Models;

/// <summary>
/// Chat message role strings — match <see cref="Microsoft.Extensions.AI.ChatRole"/> values
/// (<c>user</c>, <c>assistant</c>, <c>system</c>, <c>tool</c>) for DB storage and MAF interop.
/// </summary>
public static class ChatMessageRoles
{
    public const string User = "user";

    public const string Assistant = "assistant";

    public const string System = "system";

    public const string Tool = "tool";
}
