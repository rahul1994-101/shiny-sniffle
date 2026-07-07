namespace Application.Utilities.Helpers;

/// <summary>Layer 0 read limits and shared copy for mailbox list/status tools.</summary>
internal static class EmailReadConstants
{
    internal const int DefaultListLimit = 20;

    /// <summary>Layer 6a: default list size for digest/triage skim.</summary>
    internal const int DefaultDigestListLimit = DefaultListLimit;

    internal const int MinListLimit = 1;

    internal const int MaxListLimit = 50;

    /// <summary>Layer 6a: max get_inbox_message calls the Email agent should make per user turn.</summary>
    internal const int MaxDeepReadsPerTurn = 5;

    /// <summary>Layer 6a: optional full reads when previews are enough for digest mode.</summary>
    internal const int MaxDigestOptionalGets = 3;

    internal const int SnippetMaxLength = 120;

    internal const string SettingsEmailHint = "Connect your mailbox in Settings → Email.";

    internal const string NotConfiguredForAgent =
        $"Mailbox is not configured. {SettingsEmailHint}";

    internal const string NotConfiguredForList =
        $"Mailbox is not configured. {SettingsEmailHint} Then ask again to list mail.";

    internal const string NotConfiguredForSend =
        $"Mailbox is not configured. {SettingsEmailHint} Before sending mail.";

    internal const string NotConfiguredForGet =
        $"Mailbox is not configured. {SettingsEmailHint} Then ask again to read a message.";

    internal const string NotConfiguredForFolders =
        $"Mailbox is not configured. {SettingsEmailHint} Then ask again to list folders.";

    internal const int MaxMessageBodyLength = 12_000;

    internal const string SinceParseHint =
        "Could not parse the date range. Use today, yesterday, this_week, last_N_days, yyyy-MM-dd..yyyy-MM-dd, " +
        "yyyy-MM-dd to yyyy-MM-dd, or since + until (yyyy-MM-dd).";

    internal static string FormatSinceParseHint() =>
        $"{SinceParseHint} Today (UTC) is {EmailReadDateContext.TodayUtcIso}. Example range: 2026-05-01..2026-05-07.";

    internal static int ClampListLimit(int limit) =>
        limit <= 0 ? DefaultListLimit : Math.Clamp(limit, MinListLimit, MaxListLimit);
}
