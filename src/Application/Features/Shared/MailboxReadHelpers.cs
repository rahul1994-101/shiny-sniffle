using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Application.Features.Workspace.EmailAccounts;
using Infrastructure.Mailbox;

namespace Application.Features.Shared;

internal sealed record MailboxDateRange(
    DateTime SinceUtc,
    DateTime? UntilUtcExclusive,
    string Label);

/// <summary>Shared copy and agent-only limits for mailbox list/status tools. Caps live in <see cref="MailboxLimits"/>.</summary>
internal static class EmailReadConstants
{
    internal const int MaxDeepReadsPerTurn = 5;

    internal const int MaxDigestOptionalGets = 3;

    internal const int MaxAttachmentTextPreviewBytes = 8_192;

    internal const string SinceParseHint =
        "Could not parse the date range. Use today, yesterday, this_week, last_N_days, yyyy-MM-dd..yyyy-MM-dd, " +
        "yyyy-MM-dd to yyyy-MM-dd, or since + until (yyyy-MM-dd).";

    internal static string FormatSinceParseHint() =>
        $"{SinceParseHint} Today (UTC) is {EmailReadDateContext.TodayUtcIso}. Example range: 2026-05-01..2026-05-07.";

    internal static string FormatMailboxHeader(MailboxAccountContext account) =>
        $"Account: {account.Alias} ({account.EmailAddress}){(account.IsDefault ? " · default" : string.Empty)}";
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

internal static partial class MailboxListRangeParser
{
    private static readonly string[] RangeSeparators = ["..", " through ", " to ", " until ", " - ", "–", "—"];

    [GeneratedRegex(@"^last[_\s-]?(\d+)[_\s-]?days?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LastNDaysPattern();

    [GeneratedRegex(@"(\d{4}-\d{2}-\d{2})", RegexOptions.CultureInvariant)]
    private static partial Regex IsoDateTokenPattern();

    #region # Public API

    internal static bool TryParse(string? since, out MailboxDateRange? range) =>
        TryParse(since, null, out range);

    internal static bool TryParse(string? since, string? until, out MailboxDateRange? range)
    {
        since = NormalizeSince(since);
        until = NullIfWhiteSpace(until);
        var today = DateTime.UtcNow.Date;

        if (string.IsNullOrWhiteSpace(since) && until is null)
        {
            range = new MailboxDateRange(today, null, "today");
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
            range = new MailboxDateRange(today, null, "today");
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
            range = new MailboxDateRange(today.AddDays(-days), null, $"last {days} days");
            return true;
        }

        switch (lower)
        {
            case "today":
            case "recent":
                range = new MailboxDateRange(today, null, "today");
                return true;
            case "yesterday":
                range = new MailboxDateRange(today.AddDays(-1), today, "yesterday");
                return true;
            case "last_week":
            case "last week":
                range = new MailboxDateRange(today.AddDays(-7), null, "last week");
                return true;
            case "this_week":
            case "this week":
                range = new MailboxDateRange(StartOfWeekUtc(today), null, "this week");
                return true;
        }

        if (TryParseSingleDate(value, out var singleDay))
        {
            range = new MailboxDateRange(
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
            return parsed == today
                ? "today"
                : parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
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

    #endregion

    #region # Range parsing

    private static bool TryParseExplicitUntil(string? since, string? until, out MailboxDateRange? range)
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

    private static MailboxDateRange ToInclusiveRange(DateTime start, DateTime endInclusive) =>
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

    #endregion
}

/// <summary>Parsed, validated list query — infra filters plus agent-facing label.</summary>
internal sealed record MailboxListQuery(ListMessagesFilters Filters, string QueryLabel);

internal static class ListMessagesQueryBuilder
{
    internal static bool TryBuild(
        string since,
        string until,
        int limit,
        int skip,
        bool countOnly,
        bool unreadOnly,
        string fromSender,
        string subjectContains,
        string bodyContains,
        string toContains,
        string attachmentsFilter,
        string folder,
        out MailboxListQuery? query,
        out string? error)
    {
        if (!MailboxListRangeParser.TryParse(since, until, out var range) || range is null)
        {
            query = null;
            error = EmailReadConstants.FormatSinceParseHint();
            return false;
        }

        if (!TryParseAttachmentsFilter(attachmentsFilter, out var hasAttachments, out error))
        {
            query = null;
            return false;
        }

        var filters = new ListMessagesFilters
        {
            SinceUtc = range.SinceUtc,
            UntilUtcExclusive = range.UntilUtcExclusive,
            Limit = MailboxLimits.ClampListLimit(limit),
            Skip = MailboxLimits.ClampListSkip(skip),
            CountOnly = countOnly,
            UnreadOnly = unreadOnly,
            FromContains = NullIfWhiteSpace(fromSender),
            SubjectContains = NullIfWhiteSpace(subjectContains),
            BodyContains = NullIfWhiteSpace(bodyContains),
            ToContains = NullIfWhiteSpace(toContains),
            HasAttachments = hasAttachments,
            Folder = NullIfWhiteSpace(folder)
        };
        query = new MailboxListQuery(filters, EmailMailboxTextHelpers.FormatMailboxQueryLabel(range.Label, filters));
        error = null;
        return true;
    }

    private static bool TryParseAttachmentsFilter(string? attachmentsFilter, out bool? hasAttachments, out string? error)
    {
        hasAttachments = null;
        error = null;

        if (string.IsNullOrWhiteSpace(attachmentsFilter))
        {
            return true;
        }

        switch (attachmentsFilter.Trim().ToLowerInvariant())
        {
            case "yes":
            case "with":
            case "with_attachments":
            case "true":
                hasAttachments = true;
                return true;
            case "no":
            case "without":
            case "without_attachments":
            case "false":
                hasAttachments = false;
                return true;
            default:
                error = "attachments_filter must be empty (any), yes/with_attachments, or no/without_attachments.";
                return false;
        }
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Ordered Uids from the most recent non–count-only list in an agent turn.</summary>
internal sealed record MailboxListSnapshot(
    ListMessagesFilters Filters,
    IReadOnlyList<uint> Uids,
    string? MailboxAlias)
{
    internal static MailboxListSnapshot From(ListMessagesFilters filters, ListMessagesResult result, string? mailboxAlias) =>
        new(filters, result.Messages.Select(m => m.Uid).ToList(), mailboxAlias);

    internal bool MatchesMailbox(string? mailboxAlias) =>
        string.Equals(MailboxAlias ?? string.Empty, mailboxAlias ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    internal bool TryGetUid(int listIndex, out uint uid, out string? error)
    {
        if (listIndex < 1 || listIndex > Uids.Count)
        {
            uid = 0;
            error =
                $"List index #{listIndex} is out of range ({Uids.Count} message(s) in the recent list). List again or use a Uid.";
            return false;
        }

        uid = Uids[listIndex - 1];
        error = null;
        return true;
    }
}

/// <summary>Resolved message target — uid or list row from session snapshot.</summary>
internal sealed record MailboxOpenRequest(MessageKey Message);

internal static class MailboxOpenRequestBuilder
{
    internal static bool TryResolve(
        uint uid,
        int listIndex,
        string? folder,
        string? mailboxAlias,
        MailboxListSnapshot? lastList,
        out MailboxOpenRequest? request,
        out string? error)
    {
        request = null;
        error = null;
        var folderOverride = NullIfWhiteSpace(folder);

        if (uid > 0)
        {
            request = new MailboxOpenRequest(new MessageKey { Uid = uid, Folder = folderOverride });
            return true;
        }

        if (listIndex >= 1)
        {
            if (lastList is null)
            {
                error =
                    "No recent list in this turn. Call list_inbox_messages first, or open by Uid from a list row.";
                return false;
            }

            if (!lastList.MatchesMailbox(mailboxAlias))
            {
                error =
                    "The recent list is for a different mailbox account. List again on this account or use a Uid.";
                return false;
            }

            if (!lastList.TryGetUid(listIndex, out var resolvedUid, out error))
            {
                return false;
            }

            request = new MailboxOpenRequest(new MessageKey
            {
                Uid = resolvedUid,
                Folder = folderOverride ?? lastList.Filters.Folder
            });
            return true;
        }

        error = "Provide uid from a list row, or list_index (1-based) after list_inbox_messages in this turn.";
        return false;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal static class MessageBatchFiltersBuilder
{
    internal static bool TryBuild(
        string uidsCsv,
        string? folder,
        out MessageBatchFilters? filters,
        out string? error)
    {
        filters = null;
        error = null;

        if (!TryParseUids(uidsCsv, out var uids, out error))
        {
            return false;
        }

        var normalizedFolder = NullIfWhiteSpace(folder);
        filters = new MessageBatchFilters
        {
            Messages = uids.Select(uid => new MessageKey { Uid = uid, Folder = normalizedFolder }).ToList()
        };
        return true;
    }

    internal static bool TryParseUids(string uidsCsv, out List<uint> uids, out string? error)
    {
        uids = [];
        error = null;

        if (string.IsNullOrWhiteSpace(uidsCsv))
        {
            error = "Provide at least one Uid from list_inbox_messages (comma-separated, e.g. 42,43).";
            return false;
        }

        foreach (var part in uidsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!uint.TryParse(part, out var uid) || uid == 0)
            {
                error = $"Invalid Uid '{part}'. Use numeric Uids from list_inbox_messages.";
                uids = [];
                return false;
            }

            uids.Add(uid);
        }

        if (uids.Count == 0)
        {
            error = "Provide at least one Uid from list_inbox_messages (comma-separated, e.g. 42,43).";
            return false;
        }

        return true;
    }

    internal static bool TryParseFlagAction(string flagAction, out MessageFlagAction flag, out string? error)
    {
        flag = default;
        error = null;

        if (string.IsNullOrWhiteSpace(flagAction))
        {
            error = "Flag action is required: read, unread, flagged, or unflagged.";
            return false;
        }

        switch (flagAction.Trim().ToLowerInvariant())
        {
            case "read":
                flag = MessageFlagAction.Read;
                return true;
            case "unread":
                flag = MessageFlagAction.Unread;
                return true;
            case "flagged":
                flag = MessageFlagAction.Flagged;
                return true;
            case "unflagged":
                flag = MessageFlagAction.Unflagged;
                return true;
            default:
                error = "Flag action must be read, unread, flagged, or unflagged.";
                return false;
        }
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal static class MessageTransferFiltersBuilder
{
    internal static bool TryBuild(
        string uidsCsv,
        string? folder,
        string destinationFolder,
        out MessageTransferFilters? filters,
        out string? error)
    {
        filters = null;
        error = null;

        if (string.IsNullOrWhiteSpace(destinationFolder))
        {
            error = "Destination folder is required.";
            return false;
        }

        if (!MessageBatchFiltersBuilder.TryBuild(uidsCsv, folder, out var batch, out error))
        {
            return false;
        }

        filters = new MessageTransferFilters
        {
            Messages = batch!.Messages,
            DestinationFolder = destinationFolder.Trim()
        };
        return true;
    }
}

internal static class SetMessageFlagsFiltersBuilder
{
    internal static bool TryBuild(
        string uidsCsv,
        string? folder,
        string flagAction,
        out SetMessageFlagsFilters? filters,
        out string? error)
    {
        filters = null;

        if (!MessageBatchFiltersBuilder.TryBuild(uidsCsv, folder, out var batch, out error))
        {
            return false;
        }

        if (!MessageBatchFiltersBuilder.TryParseFlagAction(flagAction, out var flag, out error))
        {
            return false;
        }

        filters = new SetMessageFlagsFilters
        {
            Messages = batch!.Messages,
            Flag = flag
        };
        return true;
    }
}

internal static class OutboundMailBuilder
{
    internal static bool TryBuild(
        string to,
        string cc,
        string bcc,
        string subject,
        string body,
        string htmlBody,
        string mode,
        uint replyUid,
        string replyFolder,
        string attachments,
        out OutboundMail? mail,
        out string? error) =>
        TryBuild(to, cc, bcc, subject, body, htmlBody, mode, replyUid, replyFolder, attachments, forDraft: false, out mail, out error);

    internal static bool TryBuildForDraft(
        string to,
        string cc,
        string bcc,
        string subject,
        string body,
        string htmlBody,
        string mode,
        uint replyUid,
        string replyFolder,
        string attachments,
        out OutboundMail? mail,
        out string? error)
    {
        if (!TryBuild(to, cc, bcc, subject, body, htmlBody, mode, replyUid, replyFolder, attachments, forDraft: true, out mail, out error))
        {
            return false;
        }

        if (mail!.Mode == OutboundMailMode.New &&
            string.IsNullOrWhiteSpace(mail.To) &&
            mail.Cc.Count == 0 &&
            mail.Bcc.Count == 0 &&
            string.IsNullOrWhiteSpace(mail.Subject) &&
            string.IsNullOrWhiteSpace(mail.Body) &&
            string.IsNullOrWhiteSpace(mail.HtmlBody) &&
            mail.Attachments.Count == 0)
        {
            error = "Draft needs at least one of: to/cc/bcc, subject, body, html_body, or attachments.";
            mail = null;
            return false;
        }

        return true;
    }

    private static bool TryBuild(
        string to,
        string cc,
        string bcc,
        string subject,
        string body,
        string htmlBody,
        string mode,
        uint replyUid,
        string replyFolder,
        string attachments,
        bool forDraft,
        out OutboundMail? mail,
        out string? error)
    {
        mail = null;
        error = null;

        if (!TryParseMode(mode, out var mailMode, out error))
        {
            return false;
        }

        if (mailMode is OutboundMailMode.Reply or OutboundMailMode.Forward)
        {
            if (replyUid == 0)
            {
                error = "reply_uid is required for reply or forward mode (Uid from get_inbox_message or list row).";
                return false;
            }
        }

        var normalizedTo = NullIfWhiteSpace(to) ?? string.Empty;
        if (!forDraft &&
            mailMode == OutboundMailMode.New &&
            string.IsNullOrWhiteSpace(normalizedTo))
        {
            var ccList = ParseAddressList(cc);
            var bccList = ParseAddressList(bcc);
            if (ccList.Count == 0 && bccList.Count == 0)
            {
                error = "At least one recipient is required (to, cc, or bcc).";
                return false;
            }
        }

        if (!forDraft && mailMode == OutboundMailMode.New && string.IsNullOrWhiteSpace(subject))
        {
            error = "Subject is required.";
            return false;
        }

        if (!TryParseAddressList(normalizedTo, "to", out var toError))
        {
            error = toError;
            return false;
        }

        if (!TryParseAddressList(cc, "cc", out error))
        {
            return false;
        }

        if (!TryParseAddressList(bcc, "bcc", out error))
        {
            return false;
        }

        if (!TryParseAttachments(attachments, out var parsedAttachments, out error))
        {
            return false;
        }

        mail = new OutboundMail
        {
            To = normalizedTo,
            Cc = ParseAddressList(cc),
            Bcc = ParseAddressList(bcc),
            Subject = subject?.Trim() ?? string.Empty,
            Body = body ?? string.Empty,
            HtmlBody = NullIfWhiteSpace(htmlBody),
            Mode = mailMode,
            InReplyTo = mailMode is OutboundMailMode.Reply or OutboundMailMode.Forward
                ? new MessageKey { Uid = replyUid, Folder = NullIfWhiteSpace(replyFolder) }
                : null,
            Attachments = parsedAttachments
        };
        return true;
    }

    private static bool TryParseMode(string? mode, out OutboundMailMode mailMode, out string? error)
    {
        mailMode = OutboundMailMode.New;
        error = null;

        if (string.IsNullOrWhiteSpace(mode))
        {
            return true;
        }

        switch (mode.Trim().ToLowerInvariant())
        {
            case "new":
                return true;
            case "reply":
                mailMode = OutboundMailMode.Reply;
                return true;
            case "forward":
                mailMode = OutboundMailMode.Forward;
                return true;
            default:
                error = "mode must be new, reply, or forward.";
                return false;
        }
    }

    private static bool TryParseAddressList(string? addresses, string fieldName, out string? error)
    {
        error = null;
        foreach (var address in ParseAddressList(addresses))
        {
            if (!System.Net.Mail.MailAddress.TryCreate(address, out _))
            {
                error = $"{fieldName} contains an invalid email address: '{address}'.";
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<string> ParseAddressList(string? addresses)
    {
        if (string.IsNullOrWhiteSpace(addresses))
        {
            return [];
        }

        return addresses
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static bool TryParseAttachments(
        string? attachments,
        out IReadOnlyList<OutboundAttachment> parsed,
        out string? error)
    {
        parsed = [];
        error = null;

        if (string.IsNullOrWhiteSpace(attachments))
        {
            return true;
        }

        var results = new List<OutboundAttachment>();
        foreach (var entry in attachments.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = entry.IndexOf('|');
            if (separator <= 0 || separator >= entry.Length - 1)
            {
                error = "attachments format: name|base64;name2|base64 (semicolon between files, pipe between name and data).";
                parsed = [];
                return false;
            }

            var fileName = entry[..separator].Trim();
            var base64 = entry[(separator + 1)..].Trim();

            if (string.IsNullOrWhiteSpace(fileName))
            {
                error = "Each attachment entry needs a file name before the pipe.";
                parsed = [];
                return false;
            }

            byte[] content;
            try
            {
                content = Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                error = $"Attachment '{fileName}' has invalid base64 data.";
                parsed = [];
                return false;
            }

            if (content.Length > MailboxLimits.MaxOutboundAttachmentSizeBytes)
            {
                error = $"Attachment '{fileName}' exceeds the {MailboxLimits.MaxOutboundAttachmentSizeBytes / (1024 * 1024)} MB limit.";
                parsed = [];
                return false;
            }

            results.Add(new OutboundAttachment
            {
                FileName = fileName,
                Content = content
            });
        }

        if (results.Count > MailboxLimits.MaxOutboundAttachmentCount)
        {
            error = $"At most {MailboxLimits.MaxOutboundAttachmentCount} attachments are allowed per message.";
            parsed = [];
            return false;
        }

        parsed = results;
        return true;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal static class GetAttachmentsFiltersBuilder
{
    internal static bool TryBuild(
        uint uid,
        string? folder,
        int attachmentIndex,
        string attachmentName,
        out GetAttachmentsFilters? filters,
        out string? error)
    {
        filters = null;
        error = null;

        if (uid == 0)
        {
            error = "Provide uid from a list_inbox_messages row or get_inbox_message.";
            return false;
        }

        int? index = attachmentIndex >= 0 ? attachmentIndex : null;
        var name = NullIfWhiteSpace(attachmentName);

        if (index is not null && name is not null)
        {
            error = "Use attachment_index or attachment_name, not both.";
            return false;
        }

        filters = new GetAttachmentsFilters
        {
            Message = new MessageKey { Uid = uid, Folder = NullIfWhiteSpace(folder) },
            AttachmentIndex = index,
            AttachmentName = name
        };
        return true;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal static class CreateFolderFiltersBuilder
{
    internal static bool TryBuild(
        string name,
        string parentFolder,
        out CreateFolderFilters? filters,
        out string? error)
    {
        filters = null;
        error = null;

        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Folder name is required.";
            return false;
        }

        filters = new CreateFolderFilters
        {
            Name = name.Trim(),
            ParentFolder = NullIfWhiteSpace(parentFolder)
        };
        return true;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal static class EmailMailboxTextHelpers
{
    #region # List output

    internal static string FormatMailboxQueryLabel(string rangeLabel, ListMessagesFilters query)
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

        if (!string.IsNullOrWhiteSpace(query.BodyContains))
        {
            parts.Add($"body contains '{query.BodyContains.Trim()}'");
        }

        if (!string.IsNullOrWhiteSpace(query.ToContains))
        {
            parts.Add($"to contains '{query.ToContains.Trim()}'");
        }

        if (query.HasAttachments == true)
        {
            parts.Add("with attachments");
        }
        else if (query.HasAttachments == false)
        {
            parts.Add("without attachments");
        }

        if (query.Skip > 0)
        {
            parts.Add($"skip {query.Skip}");
        }

        return string.Join(", ", parts);
    }

    internal static string FormatMailboxCount(int count, string queryLabel) =>
        count == 0
            ? $"No messages found for {queryLabel}."
            : $"Message count for {queryLabel}: {count}.";

    internal static string FormatMailboxList(IReadOnlyList<MessageSummary> messages, string queryLabel, int totalMatched)
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
            $"Messages for {queryLabel} ({shownNote}; previews up to {MailboxLimits.SnippetMaxLength} chars — use get_inbox_message with folder + Uid for full body):");
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

            if (message.IsUnread)
            {
                builder.Append(" | Unread");
            }

            if (!string.IsNullOrWhiteSpace(message.Snippet))
            {
                builder.Append(" | Preview: ").Append(message.Snippet);
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    #endregion

    #region # Message output

    internal static string FormatMailboxMessages(IReadOnlyList<MessageDetail> messages)
    {
        if (messages.Count == 0)
        {
            return "No messages were found for the requested Uids.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Fetched {messages.Count} message(s):");
        for (var i = 0; i < messages.Count; i++)
        {
            if (i > 0)
            {
                builder.AppendLine();
                builder.AppendLine("---");
                builder.AppendLine();
            }

            builder.Append(FormatMailboxMessage(messages[i]));
        }

        return builder.ToString().TrimEnd();
    }

    #endregion

    #region # Other output

    internal static string FormatCommandResult(CommandResult result) =>
        result.Success
            ? result.Message
            : $"Could not complete mailbox action: {result.Message}";

    internal static string FormatMailboxMessage(MessageDetail message)
    {
        var builder = new StringBuilder();
        builder.Append("Message (folder: ").Append(message.Folder)
            .Append(", Uid: ").Append(message.Uid).AppendLine("):");
        builder.Append("From: ").AppendLine(message.From);
        builder.Append("Subject: ").AppendLine(message.Subject);
        builder.Append("Date: ").AppendLine(message.Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        builder.Append("Read: ").AppendLine(message.IsUnread ? "no" : "yes");

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

    internal static string FormatFolderList(IReadOnlyList<FolderInfo> folders)
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

    internal static string FormatFolderStats(GetFolderResult result)
    {
        var folder = result.Folder;
        var builder = new StringBuilder();
        builder.Append("Folder: ").Append(folder.Name);
        if (!folder.Name.Equals(folder.FullName, StringComparison.Ordinal))
        {
            builder.Append(" (path: ").Append(folder.FullName).Append(')');
        }

        if (!string.IsNullOrWhiteSpace(folder.Role))
        {
            builder.Append(" [").Append(folder.Role).Append(']');
        }

        builder.AppendLine();
        builder.Append("Total messages: ").AppendLine(result.TotalCount.ToString(CultureInfo.InvariantCulture));
        builder.Append("Unread: ").AppendLine(result.UnreadCount.ToString(CultureInfo.InvariantCulture));
        if (result.UidValidity is uint uidValidity)
        {
            builder.Append("Uid validity: ").Append(uidValidity);
        }

        return builder.ToString().TrimEnd();
    }

    internal static string FormatAttachments(uint uid, string folder, IReadOnlyList<AttachmentContent> attachments)
    {
        if (attachments.Count == 0)
        {
            return $"No attachments found for Uid {uid} in folder '{folder}'.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Attachments for Uid {uid} in folder '{folder}' ({attachments.Count}):");
        foreach (var attachment in attachments)
        {
            builder.Append("- [").Append(attachment.Index).Append("] ")
                .Append(attachment.FileName)
                .Append(" (").Append(attachment.ContentType)
                .Append(", ").Append(FormatByteSize(attachment.Content.Length)).AppendLine(")");

            if (TryFormatAttachmentPreview(attachment, out var preview))
            {
                builder.AppendLine("  Content preview:");
                builder.AppendLine(preview);
            }
            else
            {
                builder.AppendLine("  Content: omitted (binary or too large for preview).");
            }
        }

        return builder.ToString().TrimEnd();
    }

    internal static string FormatSaveDraftResult(SaveDraftResult result) =>
        result.Success
            ? result.Message
            : $"Could not save draft: {result.Message}";

    private static bool TryFormatAttachmentPreview(AttachmentContent attachment, out string preview)
    {
        preview = string.Empty;

        if (attachment.Content.Length == 0)
        {
            preview = "(empty file)";
            return true;
        }

        if (attachment.Content.Length > EmailReadConstants.MaxAttachmentTextPreviewBytes)
        {
            return false;
        }

        var contentType = attachment.ContentType.ToLowerInvariant();
        var fileName = attachment.FileName.ToLowerInvariant();
        var looksText = contentType.StartsWith("text/", StringComparison.Ordinal) ||
                        contentType.Contains("json", StringComparison.Ordinal) ||
                        contentType.Contains("xml", StringComparison.Ordinal) ||
                        fileName.EndsWith(".txt", StringComparison.Ordinal) ||
                        fileName.EndsWith(".csv", StringComparison.Ordinal) ||
                        fileName.EndsWith(".json", StringComparison.Ordinal) ||
                        fileName.EndsWith(".xml", StringComparison.Ordinal);

        if (!looksText)
        {
            return false;
        }

        try
        {
            preview = Encoding.UTF8.GetString(attachment.Content).TrimEnd();
            if (preview.Contains('\0'))
            {
                preview = string.Empty;
                return false;
            }

            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static string FormatByteSize(int bytes) =>
        bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
            _ => $"{bytes / (1024.0 * 1024.0):0.#} MB"
        };

    private static bool IsInboxAlias(string? folder) =>
        string.IsNullOrWhiteSpace(folder) || folder.Trim().Equals("inbox", StringComparison.OrdinalIgnoreCase);

    #endregion
}
