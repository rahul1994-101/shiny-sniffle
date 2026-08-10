namespace Application.Features.workspace.Tags.Queries;

using Application.Features.workspace.Tags;

public sealed record ListTagsRequest(Guid UserId) : IQuery<ListTagsResponse>;

public sealed class ListTagsResponse
{
    public IReadOnlyList<TagDto> Tags { get; init; } = [];
}

public sealed class ListTagsRequestHandler(TagRepository tagRepo)
    : IRequestHandler<ListTagsRequest, ListTagsResponse>
{
    public async ValueTask<Result<ListTagsResponse>> HandleAsync(
        ListTagsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<ListTagsResponse>();
        var tags = await tagRepo.ListAsync(request.UserId, cancellationToken);
        result.Success(new ListTagsResponse { Tags = tags });
        return result;
    }
}
