using MailKit.Search;

namespace Infrastructure.Mailbox;

internal static class MailboxSearchHelpers
{
    internal static SearchQuery BuildQuery(InboxQuery query)
    {
        SearchQuery search = query.SinceUtc is null
            ? SearchQuery.All
            : SearchQuery.DeliveredAfter(query.SinceUtc.Value);

        if (query.UntilUtcExclusive is not null)
        {
            search = search.And(SearchQuery.DeliveredBefore(query.UntilUtcExclusive.Value));
        }

        if (query.UnreadOnly)
        {
            search = search.And(SearchQuery.NotSeen);
        }

        if (!string.IsNullOrWhiteSpace(query.FromContains))
        {
            search = search.And(SearchQuery.FromContains(query.FromContains.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(query.SubjectContains))
        {
            search = search.And(SearchQuery.SubjectContains(query.SubjectContains.Trim()));
        }

        return search;
    }
}
