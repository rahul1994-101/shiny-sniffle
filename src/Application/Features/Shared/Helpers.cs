using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Infrastructure.Mailbox;

namespace Application.Features.Shared;

/// <summary>Shared slug rules for per-user <c>alias</c> columns (contacts, mailboxes).</summary>
public static class EntityAliasRules
{
    public const int MaxLength = 64;

    private static readonly Regex NonSlugChars = new(@"[^a-z0-9\-]+", RegexOptions.Compiled);

    private static readonly Regex CollapseHyphens = new(@"-{2,}", RegexOptions.Compiled);

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Slugify optional user-provided alias; null when blank or slugifies to empty.</summary>
    public static string? SlugifyOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var slug = SlugifySegment(value.Trim());
        slug = CollapseHyphens.Replace(NonSlugChars.Replace(slug, "-"), "-").Trim('-');
        return slug.Length > 0 ? Truncate(slug) : null;
    }

    /// <summary>Slug stem from a single display label (catalog name, etc.).</summary>
    public static string StemFromLabel(string value, string emptyFallback = "item")
    {
        var slug = SlugifySegment(value.Trim());
        return slug.Length > 0 ? Truncate(slug) : emptyFallback;
    }

    public static string StemFromPersonName(string firstName, string lastName)
    {
        var parts = new[] { firstName.Trim(), lastName.Trim() }
            .Where(x => x.Length > 0)
            .Select(SlugifySegment);

        var combined = string.Join("-", parts.Where(x => x.Length > 0));
        return combined.Length > 0 ? Truncate(combined) : "contact";
    }

    public static string StemFromEmailAddress(string emailAddress)
    {
        var trimmed = emailAddress.Trim();
        var at = trimmed.IndexOf('@');
        var local = at > 0 ? trimmed[..at] : trimmed;
        var slug = SlugifySegment(local);
        return slug.Length > 0 ? Truncate(slug) : "mailbox";
    }

    public static string WithNumericSuffix(string stem, int index, string emptyStemFallback)
    {
        if (index <= 1)
        {
            return Truncate(string.IsNullOrEmpty(stem) ? emptyStemFallback : stem);
        }

        var baseStem = string.IsNullOrEmpty(stem) ? emptyStemFallback : stem;
        var suffix = $"-{index}";
        var maxStem = MaxLength - suffix.Length;
        if (maxStem < 1)
        {
            maxStem = 1;
        }

        var trimmedStem = baseStem.Length <= maxStem ? baseStem : baseStem[..maxStem].TrimEnd('-');
        if (trimmedStem.Length == 0)
        {
            trimmedStem = emptyStemFallback;
        }

        return trimmedStem + suffix;
    }

    private static string SlugifySegment(string value)
    {
        var lower = value.ToLowerInvariant();
        var normalized = lower.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else if (char.IsWhiteSpace(ch) || ch is '-' or '_' or '.')
            {
                builder.Append('-');
            }
        }

        return CollapseHyphens.Replace(NonSlugChars.Replace(builder.ToString(), "-"), "-").Trim('-');
    }

    private static string Truncate(string value) =>
        value.Length <= MaxLength ? value : value[..MaxLength].TrimEnd('-');
}

/// <summary>
/// Typed handles for AI, tools, and working memory.
/// Plain <c>alias</c> stays in workspace tables; <c>dbo.EmailProvider.slug</c> is catalog-only (not part of this scheme).
/// Use <see cref="Format"/> / <see cref="TryParse"/> at boundaries.
/// </summary>
public static class EntityRefs
{
    public const char Separator = ':';

    public enum Kind
    {
        Contact,
        Mailbox,
        Tag,
        Bucket
    }

    public static string Format(Kind kind, string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            throw new ArgumentException("Alias is required.", nameof(alias));
        }

        return $"{Prefix(kind)}{Separator}{alias.Trim()}";
    }

    public static bool TryParse(string? value, out Kind kind, out string alias)
    {
        kind = default;
        alias = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        var separator = trimmed.IndexOf(Separator);
        if (separator <= 0 || separator >= trimmed.Length - 1)
        {
            return false;
        }

        var prefix = trimmed[..separator];
        if (!TryKindFromPrefix(prefix, out kind))
        {
            return false;
        }

        alias = trimmed[(separator + 1)..].Trim();
        return alias.Length > 0;
    }

    internal static string Prefix(Kind kind) => kind switch
    {
        Kind.Contact => "contact",
        Kind.Mailbox => "mailbox",
        Kind.Tag => "tag",
        Kind.Bucket => "bucket",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static bool TryKindFromPrefix(ReadOnlySpan<char> prefix, out Kind kind)
    {
        if (prefix.Equals("contact", StringComparison.OrdinalIgnoreCase))
        {
            kind = Kind.Contact;
            return true;
        }

        if (prefix.Equals("mailbox", StringComparison.OrdinalIgnoreCase))
        {
            kind = Kind.Mailbox;
            return true;
        }

        if (prefix.Equals("tag", StringComparison.OrdinalIgnoreCase))
        {
            kind = Kind.Tag;
            return true;
        }

        if (prefix.Equals("bucket", StringComparison.OrdinalIgnoreCase))
        {
            kind = Kind.Bucket;
            return true;
        }

        kind = default;
        return false;
    }
}

/// <summary>Preview handles in workspace forms before save (matches server slugify rules).</summary>
public static class EntityAliasPreview
{
    public static string FromNameOrAlias(string? alias, string displayName, string emptyFallback)
    {
        if (!string.IsNullOrWhiteSpace(alias))
        {
            return EntityAliasRules.StemFromLabel(alias.Trim(), emptyFallback);
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return string.Empty;
        }

        return EntityAliasRules.StemFromLabel(displayName.Trim(), emptyFallback);
    }
}

/// <summary>Shared optional presentation fields on <c>dbo</c> catalog rows (e.g. email providers).</summary>
public static partial class CatalogFieldRules
{
    public const int SlugMaxLength = 64;

    public const int NoteMaxLength = 256;

    public const int ColorMaxLength = 9;

    public static string NormalizeSlug(string? slug) =>
        string.IsNullOrWhiteSpace(slug) ? string.Empty : slug.Trim().ToLowerInvariant();

    public static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        var trimmed = color.Trim();
        return trimmed.Length > ColorMaxLength ? trimmed[..ColorMaxLength] : trimmed;
    }

    public static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();
        return trimmed.Length > NoteMaxLength ? trimmed[..NoteMaxLength] : trimmed;
    }

    public static bool IsValidSlugFormat(string slug) =>
        !string.IsNullOrEmpty(slug) && SlugRegex().IsMatch(slug);

    public static string StemFromDisplayName(string displayName) =>
        EntityAliasRules.StemFromLabel(displayName);

    public static string SlugWithNumericSuffix(string stem, int index, string emptyStemFallback) =>
        EntityAliasRules.WithNumericSuffix(stem, index, emptyStemFallback);

    public static string? ValidateSlug(string? slug, bool required = true)
    {
        var normalized = NormalizeSlug(slug);
        if (string.IsNullOrEmpty(normalized))
        {
            return required ? "Slug is required." : null;
        }

        if (normalized.Length > SlugMaxLength)
        {
            return $"Slug must be {SlugMaxLength} characters or fewer.";
        }

        if (!IsValidSlugFormat(normalized))
        {
            return "Slug must use lowercase letters, numbers, and hyphens only.";
        }

        return null;
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugRegex();
}

public static class ReferableKindMapping
{
    public static ReferableKind ToPersistence(EntityRefs.Kind kind) => kind switch
    {
        EntityRefs.Kind.Contact => ReferableKind.Contact,
        EntityRefs.Kind.Mailbox => ReferableKind.Mailbox,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}

internal static class WorkspaceErAliasResolver
{
    internal static async Task<string> ResolveAsync(
        Func<string, Guid?, CancellationToken, Task<bool>> isTakenAsync,
        string displayName,
        string? requestedAlias,
        Guid? excludeId,
        string emptyStemFallback,
        CancellationToken cancellationToken)
    {
        var normalized = EntityAliasRules.SlugifyOptional(requestedAlias);
        if (normalized is not null)
        {
            return normalized;
        }

        var stem = EntityAliasRules.StemFromLabel(displayName, emptyStemFallback);

        for (var index = 1; index < 10_000; index++)
        {
            var candidate = EntityAliasRules.WithNumericSuffix(stem, index, emptyStemFallback);
            if (!await isTakenAsync(candidate, excludeId, cancellationToken))
            {
                return candidate;
            }
        }

        return EntityAliasRules.WithNumericSuffix(stem, Random.Shared.Next(1000, 9999), emptyStemFallback);
    }
}

internal sealed record InboxDateRange(
    DateTime SinceUtc,
    DateTime? UntilUtcExclusive,
    string Label);

/// <summary>Shared copy and agent-only limits for mailbox list/status tools. Read caps live in <see cref="Infrastructure.Mailbox.MailboxReadLimits"/>.</summary>
internal static class EmailReadConstants
{
    internal const int MaxDeepReadsPerTurn = 5;

    internal const int MaxDigestOptionalGets = 3;

    internal const string WorkspaceEmailHint = "Connect your mailbox in Workspace → Email accounts.";

    internal const string NotConfiguredForAgent =
        $"Mailbox is not configured. {WorkspaceEmailHint}";

    internal const string NotConfiguredForList =
        $"Mailbox is not configured. {WorkspaceEmailHint} Then ask again to list mail.";

    internal const string NotConfiguredForSend =
        $"Mailbox is not configured. {WorkspaceEmailHint} Before sending mail.";

    internal const string NotConfiguredForGet =
        $"Mailbox is not configured. {WorkspaceEmailHint} Then ask again to read a message.";

    internal const string NotConfiguredForFolders =
        $"Mailbox is not configured. {WorkspaceEmailHint} Then ask again to list folders.";

    internal const string SinceParseHint =
        "Could not parse the date range. Use today, yesterday, this_week, last_N_days, yyyy-MM-dd..yyyy-MM-dd, " +
        "yyyy-MM-dd to yyyy-MM-dd, or since + until (yyyy-MM-dd).";

    internal static string FormatSinceParseHint() =>
        $"{SinceParseHint} Today (UTC) is {EmailReadDateContext.TodayUtcIso}. Example range: 2026-05-01..2026-05-07.";
}

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

internal static class EmailMailboxTextHelpers
{
    internal static string FormatInboxQueryLabel(string rangeLabel, InboxQuery query)
    {
        var parts = new List<string>();

        if (!IsInboxAlias(query.Folder))
        {
            parts.Add($"folder '{query.Folder!.Trim()}'");
        }

        parts.Add(rangeLabel);

        if (query.UnreadOnly)
        {
            parts.Add("unread only");
        }

        if (!string.IsNullOrWhiteSpace(query.FromContains))
        {
            parts.Add($"from contains '{query.FromContains.Trim()}'");
        }

        if (!string.IsNullOrWhiteSpace(query.SubjectContains))
        {
            parts.Add($"subject contains '{query.SubjectContains.Trim()}'");
        }

        return string.Join(", ", parts);
    }

    internal static string FormatInboxCount(int count, string queryLabel) =>
        count == 0
            ? $"No messages found for {queryLabel}."
            : $"Message count for {queryLabel}: {count}.";

    internal static string FormatInboxList(IReadOnlyList<InboxMessageSummary> messages, string queryLabel, int totalMatched)
    {
        if (messages.Count == 0)
        {
            return totalMatched == 0
                ? $"No messages found for {queryLabel}."
                : $"No messages to show for {queryLabel} ({totalMatched} matched).";
        }

        var shownNote = totalMatched > messages.Count
            ? $"{messages.Count} shown of {totalMatched} matched"
            : $"{messages.Count}";

        var builder = new StringBuilder();
        builder.AppendLine(
            $"Messages for {queryLabel} ({shownNote}; previews up to {MailboxReadLimits.SnippetMaxLength} chars — use get_inbox_message with folder + Uid for full body):");
        for (var i = 0; i < messages.Count; i++)
        {
            var message = messages[i];
            builder.Append('#').Append(i + 1)
                .Append(" | Uid: ").Append(message.Uid)
                .Append(" | From: ")
                .Append(message.From)
                .Append(" | Subject: ")
                .Append(message.Subject)
                .Append(" | Date: ")
                .Append(message.Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(message.Snippet))
            {
                builder.Append(" | Preview: ").Append(message.Snippet);
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    internal static string FormatInboxMessage(InboxMessageDetail message)
    {
        var builder = new StringBuilder();
        builder.Append("Message (folder: ").Append(message.Folder)
            .Append(", Uid: ").Append(message.Uid).AppendLine("):");
        builder.Append("From: ").AppendLine(message.From);
        builder.Append("Subject: ").AppendLine(message.Subject);
        builder.Append("Date: ").AppendLine(message.Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

        if (message.AttachmentNames.Count == 0)
        {
            builder.AppendLine("Attachments: none");
        }
        else
        {
            builder.Append("Attachments: ").AppendLine(string.Join(", ", message.AttachmentNames));
        }

        builder.AppendLine();
        builder.Append(message.BodyFromHtml ? "Body (converted from HTML):" : "Body:");
        builder.AppendLine();
        builder.Append(message.Body);
        return builder.ToString().TrimEnd();
    }

    internal static string FormatFolderList(IReadOnlyList<MailboxFolderInfo> folders)
    {
        if (folders.Count == 0)
        {
            return "No mailbox folders were found.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Mailbox folders ({folders.Count}). Use folder name or role alias (inbox, sent, drafts, trash, junk) on list/get tools:");
        foreach (var folder in folders)
        {
            builder.Append("- ").Append(folder.Name);
            if (!folder.Name.Equals(folder.FullName, StringComparison.Ordinal))
            {
                builder.Append(" (path: ").Append(folder.FullName).Append(')');
            }

            if (!string.IsNullOrWhiteSpace(folder.Role))
            {
                builder.Append(" [").Append(folder.Role).Append(']');
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private static bool IsInboxAlias(string? folder) =>
        string.IsNullOrWhiteSpace(folder) || folder.Trim().Equals("inbox", StringComparison.OrdinalIgnoreCase);
}
