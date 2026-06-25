using System.Globalization;

namespace WebApp.Utilities.Helpers;

/// <summary>Current UTC calendar for Email agent/tools — avoids wrong-year ISO dates from the model.</summary>
internal static class EmailReadDateContext
{
    internal static DateTime TodayUtc => DateTime.UtcNow.Date;

    internal static string TodayUtcIso =>
        TodayUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    internal static int CurrentYear => TodayUtc.Year;

    internal static string AgentDateBlock()
    {
        var now = DateTime.UtcNow;
        return
            $"Current date (UTC): {TodayUtcIso} ({now:dddd}). Current year: {CurrentYear}. " +
            "Use this when choosing since values or interpreting the user's time words.";
    }

    internal static string SinceToolHint() =>
        "Relative: today, yesterday, this_week, last_week, last_N_days. " +
        "Explicit range: yyyy-MM-dd..yyyy-MM-dd, yyyy-MM-dd to yyyy-MM-dd, or since=yyyy-MM-dd + until=yyyy-MM-dd. " +
        $"Today (UTC) is {TodayUtcIso}. Empty since means today.";
}
