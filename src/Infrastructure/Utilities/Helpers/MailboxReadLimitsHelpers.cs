namespace Infrastructure.Utilities.Helpers;

internal static class MailboxReadLimitsHelpers
{
    internal const int DefaultListLimit = 20;

    internal const int MinListLimit = 1;

    internal const int MaxListLimit = 50;

    internal const int SnippetMaxLength = 120;

    internal const int MaxMessageBodyLength = 12_000;

    internal static int ClampListLimit(int limit) =>
        limit <= 0 ? DefaultListLimit : Math.Clamp(limit, MinListLimit, MaxListLimit);
}
