using System.Globalization;
using System.Text.RegularExpressions;

namespace WebApp.Utilities.Helpers;

internal static partial class InboxListRangeParser
{
    private static readonly string[] RangeSeparators = ["..", " through ", " to ", " until ", " - ", "–", "—"];

    [GeneratedRegex(@"^last[_\s-]?(\d+)[_\s-]?days?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LastNDaysPattern();

    [GeneratedRegex(@"(\d{4}-\d{2}-\d{2})", RegexOptions.CultureInvariant)]
    private static partial Regex IsoDateTokenPattern();

    internal static bool TryParse(string? since, out InboxDateRange? range) =>
        TryParse(since, null, out range);

    internal static bool TryParse(string? since, string? until, out InboxDateRange? range)
    {
        since = NormalizeSince(since);
        until = NullIfWhiteSpace(until);
        var today = DateTime.UtcNow.Date;

        if (string.IsNullOrWhiteSpace(since) && until is null)
        {
            range = new InboxDateRange(today, null, "today");
            return true;
        }

        if (TryParseExplicitUntil(since, until, out range))
        {
            return true;
        }

        if (until is not null)
        {
            range = null;
            return false;
        }

        if (string.IsNullOrWhiteSpace(since))
        {
            range = new InboxDateRange(today, null, "today");
            return true;
        }

        var value = SanitizeRangeInput(since.Trim());
        var lower = value.ToLowerInvariant();

        if (TryParseDateRange(value, out var rangeStart, out var rangeEndInclusive))
        {
            range = ToInclusiveRange(rangeStart, rangeEndInclusive);
            return true;
        }

        if (TryParseLastNDays(lower, out var days))
        {
            range = new InboxDateRange(today.AddDays(-days), null, $"last {days} days");
            return true;
        }

        switch (lower)
        {
            case "today":
            case "recent":
                range = new InboxDateRange(today, null, "today");
                return true;
            case "yesterday":
                range = new InboxDateRange(today.AddDays(-1), today, "yesterday");
                return true;
            case "last_week":
            case "last week":
                range = new InboxDateRange(today.AddDays(-7), null, "last week");
                return true;
            case "this_week":
            case "this week":
                range = new InboxDateRange(StartOfWeekUtc(today), null, "this week");
                return true;
        }

        if (TryParseSingleDate(value, out var singleDay))
        {
            range = new InboxDateRange(
                singleDay,
                singleDay.AddDays(1),
                singleDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            return true;
        }

        range = null;
        return false;
    }

    /// <summary>Correct common model mistakes (wrong year on ISO dates, future dates) before parse.</summary>
    internal static string? NormalizeSince(string? since)
    {
        if (string.IsNullOrWhiteSpace(since))
        {
            return since;
        }

        var value = SanitizeRangeInput(since.Trim());
        var lower = value.ToLowerInvariant();

        if (lower is "today" or "recent" or "yesterday" or "last_week" or "last week" or "this_week" or "this week")
        {
            return lower;
        }

        if (TryParseLastNDays(lower, out _))
        {
            return lower;
        }

        if (TryParseDateRange(value, out var rangeStart, out var rangeEnd))
        {
            return $"{rangeStart:yyyy-MM-dd}..{rangeEnd:yyyy-MM-dd}";
        }

        if (!TryParseSingleDate(value, out var parsed))
        {
            return value;
        }

        var today = DateTime.UtcNow.Date;

        if (parsed >= today)
        {
            return parsed == today ? "today" : "today";
        }

        if (parsed == today.AddDays(-1))
        {
            return "yesterday";
        }

        if (parsed.Year != today.Year)
        {
            var sameMonthDayThisYear = new DateTime(today.Year, parsed.Month, parsed.Day, 0, 0, 0, DateTimeKind.Utc);
            if (sameMonthDayThisYear < today)
            {
                return sameMonthDayThisYear.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }

        return value;
    }

    private static bool TryParseExplicitUntil(string? since, string? until, out InboxDateRange? range)
    {
        range = null;
        if (until is null)
        {
            return false;
        }

        var startText = string.IsNullOrWhiteSpace(since) ? null : SanitizeRangeInput(since.Trim());
        if (startText is null || !TryParseSingleDate(startText, out var start))
        {
            return false;
        }

        if (!TryParseSingleDate(SanitizeRangeInput(until.Trim()), out var endInclusive))
        {
            return false;
        }

        if (endInclusive < start)
        {
            return false;
        }

        range = ToInclusiveRange(start, endInclusive);
        return true;
    }

    private static InboxDateRange ToInclusiveRange(DateTime start, DateTime endInclusive) =>
        new(
            start,
            endInclusive.AddDays(1),
            $"{start:yyyy-MM-dd} to {endInclusive:yyyy-MM-dd}");

    private static string SanitizeRangeInput(string value)
    {
        value = value.Trim().TrimEnd('.');

        foreach (var prefix in new[] { "from ", "between ", "during ", "messages from ", "mail from ", "emails from " })
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = value[prefix.Length..].Trim();
            }
        }

        return value;
    }

    private static bool TryParseDateRange(string value, out DateTime start, out DateTime endInclusive)
    {
        start = default;
        endInclusive = default;

        value = SanitizeRangeInput(value);

        if (TryParseIsoDatePair(value, out start, out endInclusive))
        {
            return endInclusive >= start;
        }

        foreach (var separator in RangeSeparators)
        {
            var index = value.IndexOf(separator, StringComparison.OrdinalIgnoreCase);
            if (index <= 0)
            {
                continue;
            }

            var left = SanitizeRangeInput(value[..index].Trim());
            var right = SanitizeRangeInput(value[(index + separator.Length)..].Trim());
            if (TryParseSingleDate(left, out start) && TryParseSingleDate(right, out endInclusive) && endInclusive >= start)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseIsoDatePair(string value, out DateTime start, out DateTime endInclusive)
    {
        start = default;
        endInclusive = default;

        var matches = IsoDateTokenPattern().Matches(value);
        if (matches.Count < 2)
        {
            return false;
        }

        if (!TryParseIsoDateToken(matches[0].Value, out start) || !TryParseIsoDateToken(matches[1].Value, out endInclusive))
        {
            return false;
        }

        return true;
    }

    private static bool TryParseIsoDateToken(string token, out DateTime dateUtc)
    {
        if (DateOnly.TryParseExact(token, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
        {
            dateUtc = dateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            return true;
        }

        return TryParseSingleDate(token, out dateUtc);
    }

    private static bool TryParseLastNDays(string lower, out int days)
    {
        days = 0;
        var match = LastNDaysPattern().Match(lower);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out days))
        {
            return false;
        }

        return days is >= 1 and <= 365;
    }

    private static bool TryParseSingleDate(string value, out DateTime dateUtc)
    {
        value = SanitizeRangeInput(value);

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var isoDate))
        {
            dateUtc = isoDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            return true;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            dateUtc = parsed.ToUniversalTime().Date;
            return true;
        }

        dateUtc = default;
        return false;
    }

    private static DateTime StartOfWeekUtc(DateTime today)
    {
        var offset = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return today.AddDays(-offset);
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
