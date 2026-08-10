namespace Application.Features.workspace.Buckets.Queries;

using Application.Features.workspace.Buckets;

public sealed record ListBucketsRequest(Guid UserId) : IQuery<ListBucketsResponse>;

public sealed class ListBucketsResponse
{
    public IReadOnlyList<BucketDto> Buckets { get; init; } = [];
}

public sealed class ListBucketsRequestHandler(BucketRepository bucketRepo)
    : IRequestHandler<ListBucketsRequest, ListBucketsResponse>
{
    public async ValueTask<Result<ListBucketsResponse>> HandleAsync(
        ListBucketsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<ListBucketsResponse>();
        var buckets = await bucketRepo.ListAsync(request.UserId, cancellationToken);
        result.Success(new ListBucketsResponse { Buckets = buckets });
        return result;
    }
}
