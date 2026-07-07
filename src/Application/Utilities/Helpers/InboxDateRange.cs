namespace Application.Utilities.Helpers;

internal sealed record InboxDateRange(
    DateTime SinceUtc,
    DateTime? UntilUtcExclusive,
    string Label);
