namespace Application.Features.workspace.Buckets.Commands;

public sealed record DeleteBucketRequest(Guid UserId, Guid BucketId) : ICommand<DeleteBucketResponse>;

public sealed class DeleteBucketResponse;

public sealed class DeleteBucketRequestHandler(BucketRepository bucketRepo)
    : IRequestHandler<DeleteBucketRequest, DeleteBucketResponse>
{
    public async ValueTask<Result<DeleteBucketResponse>> HandleAsync(
        DeleteBucketRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<DeleteBucketResponse>();
        var deleted = await bucketRepo.SoftDeleteAsync(request.UserId, request.BucketId, request.UserId, cancellationToken);
        if (!deleted)
        {
            result.Failure(ErrorCode.NotFound, "Bucket not found.");
        }
        else
        {
            result.Success(new DeleteBucketResponse());
        }

        return result;
    }
}
