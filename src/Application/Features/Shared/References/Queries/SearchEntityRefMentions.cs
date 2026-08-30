using Application.Features.Workspace.Buckets;
using Application.Features.Workspace.Contacts;
using Application.Features.Workspace.EmailAccounts;
using Application.Features.Workspace.Tags;
using FluentValidation;

namespace Application.Features.Shared.Queries;

public sealed record SearchEntityRefMentionsRequest(
    Guid UserId,
    EntityRefs.Kind Kind,
    string? Query,
    IReadOnlyList<string>? RecentHandles = null,
    int Limit = EntityRefMentionSearch.DefaultLimit) : IQuery<SearchEntityRefMentionsResponse>;

public sealed class SearchEntityRefMentionsResponse
{
    public IReadOnlyList<EntityRefMentionItemDto> Items { get; init; } = [];

    public int TotalCount { get; init; }
}

public sealed class SearchEntityRefMentionsRequestValidator : AbstractValidator<SearchEntityRefMentionsRequest>
{
    public SearchEntityRefMentionsRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, EntityRefMentionSearch.MaxLimit)
            .WithMessage($"Limit must be between 1 and {EntityRefMentionSearch.MaxLimit}.");
    }
}

public sealed class SearchEntityRefMentionsRequestHandler(
    ContactRepository contactRepo,
    EmailAccountRepository emailAccountRepo,
    TagRepository tagRepo,
    BucketRepository bucketRepo)
    : IRequestHandler<SearchEntityRefMentionsRequest, SearchEntityRefMentionsResponse>
{
    public async ValueTask<Result<SearchEntityRefMentionsResponse>> HandleAsync(
        SearchEntityRefMentionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<SearchEntityRefMentionsResponse>();
        var recentAliases = EntityRefMentionSearch.ExtractRecentAliases(request.Kind, request.RecentHandles);

        var (items, totalCount) = request.Kind switch
        {
            EntityRefs.Kind.Contact => await contactRepo.SearchMentionItemsAsync(
                request.UserId,
                request.Query,
                recentAliases,
                request.Limit,
                cancellationToken),
            EntityRefs.Kind.Mailbox => await emailAccountRepo.SearchMentionItemsAsync(
                request.UserId,
                request.Query,
                recentAliases,
                request.Limit,
                cancellationToken),
            EntityRefs.Kind.Tag => await tagRepo.SearchMentionItemsAsync(
                request.UserId,
                request.Query,
                recentAliases,
                request.Limit,
                cancellationToken),
            EntityRefs.Kind.Bucket => await bucketRepo.SearchMentionItemsAsync(
                request.UserId,
                request.Query,
                recentAliases,
                request.Limit,
                cancellationToken),
            _ => ([], 0)
        };

        result.Success(new SearchEntityRefMentionsResponse
        {
            Items = items,
            TotalCount = totalCount
        });

        return result;
    }
}

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
