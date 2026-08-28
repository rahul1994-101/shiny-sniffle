namespace Application.Features.Shared.Queries;

using Application.Features.Workspace.Buckets;
using Application.Features.Workspace.Contacts;
using Application.Features.Workspace.EmailAccounts;
using Application.Features.Workspace.Tags;
using FluentValidation;

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
