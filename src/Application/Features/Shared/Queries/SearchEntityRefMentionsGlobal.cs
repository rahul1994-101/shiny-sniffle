namespace Application.Features.Shared.Queries;

using Application.Features.Workspace.Buckets;
using Application.Features.Workspace.Contacts;
using Application.Features.Workspace.EmailAccounts;
using Application.Features.Workspace.Tags;
using FluentValidation;

public sealed record SearchEntityRefMentionsGlobalRequest(
    Guid UserId,
    string? Query,
    IReadOnlyList<EntityRefs.Kind> EnabledKinds,
    IReadOnlyList<string>? RecentHandles = null,
    int Limit = EntityRefMentionSearch.DefaultLimit) : IQuery<SearchEntityRefMentionsGlobalResponse>;

public sealed class SearchEntityRefMentionsGlobalResponse
{
    public IReadOnlyList<EntityRefMentionItemDto> Items { get; init; } = [];

    public int TotalCount { get; init; }
}

public sealed class SearchEntityRefMentionsGlobalRequestValidator : AbstractValidator<SearchEntityRefMentionsGlobalRequest>
{
    public SearchEntityRefMentionsGlobalRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.EnabledKinds).NotEmpty().WithMessage("At least one entity kind is required.");
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, EntityRefMentionSearch.MaxLimit)
            .WithMessage($"Limit must be between 1 and {EntityRefMentionSearch.MaxLimit}.");
    }
}

public sealed class SearchEntityRefMentionsGlobalRequestHandler(
    ContactRepository contactRepo,
    EmailAccountRepository emailAccountRepo,
    TagRepository tagRepo,
    BucketRepository bucketRepo)
    : IRequestHandler<SearchEntityRefMentionsGlobalRequest, SearchEntityRefMentionsGlobalResponse>
{
    public async ValueTask<Result<SearchEntityRefMentionsGlobalResponse>> HandleAsync(
        SearchEntityRefMentionsGlobalRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<SearchEntityRefMentionsGlobalResponse>();
        var perKindLimit = Math.Max(request.Limit, EntityRefMentionSearch.DefaultLimit);
        var searchTasks = request.EnabledKinds
            .Select(kind => SearchKindAsync(kind, request, perKindLimit, cancellationToken))
            .ToArray();

        var kindResults = await Task.WhenAll(searchTasks);
        var allItems = new List<EntityRefMentionItemDto>();
        var totalCount = 0;

        foreach (var (items, count) in kindResults)
        {
            allItems.AddRange(items);
            totalCount += count;
        }

        var merged = EntityRefMentionSearch.MergeGlobalResults(
            allItems,
            request.Query,
            request.RecentHandles,
            request.Limit);

        result.Success(new SearchEntityRefMentionsGlobalResponse
        {
            Items = merged,
            TotalCount = totalCount
        });

        return result;
    }

    private async Task<(IReadOnlyList<EntityRefMentionItemDto> Items, int TotalCount)> SearchKindAsync(
        EntityRefs.Kind kind,
        SearchEntityRefMentionsGlobalRequest request,
        int perKindLimit,
        CancellationToken cancellationToken)
    {
        var recentAliases = EntityRefMentionSearch.ExtractRecentAliases(kind, request.RecentHandles);

        return kind switch
        {
            EntityRefs.Kind.Contact => await contactRepo.SearchMentionItemsAsync(
                request.UserId,
                request.Query,
                recentAliases,
                perKindLimit,
                cancellationToken),
            EntityRefs.Kind.Mailbox => await emailAccountRepo.SearchMentionItemsAsync(
                request.UserId,
                request.Query,
                recentAliases,
                perKindLimit,
                cancellationToken),
            EntityRefs.Kind.Tag => await tagRepo.SearchMentionItemsAsync(
                request.UserId,
                request.Query,
                recentAliases,
                perKindLimit,
                cancellationToken),
            EntityRefs.Kind.Bucket => await bucketRepo.SearchMentionItemsAsync(
                request.UserId,
                request.Query,
                recentAliases,
                perKindLimit,
                cancellationToken),
            _ => ([], 0)
        };
    }
}
