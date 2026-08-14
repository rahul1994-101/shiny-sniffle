namespace Application.Features.Workspace.Buckets.Commands;

using Application.Features.Workspace.Buckets;
using FluentValidation;

public sealed record DeleteBucketRequest(Guid UserId, Guid BucketId) : ICommand<DeleteBucketResponse>;

public sealed class DeleteBucketResponse;

public sealed class DeleteBucketRequestValidator : AbstractValidator<DeleteBucketRequest>
{
    public DeleteBucketRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.BucketId).NotEmpty().WithMessage("Bucket Id is required.");
    }
}

public sealed class DeleteBucketRequestHandler(BucketRepository bucketRepo)
    : IRequestHandler<DeleteBucketRequest, DeleteBucketResponse>
{
    public async ValueTask<Result<DeleteBucketResponse>> HandleAsync(
        DeleteBucketRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<DeleteBucketResponse>();

        #region # Execute

        var deleted = await bucketRepo.SoftDeleteAsync(request.UserId, request.BucketId, request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        if (!deleted)
        {
            result.Failure(ErrorCode.NotFound, "Bucket not found.");
        }
        else
        {
            result.Success(new DeleteBucketResponse());
        }

        #endregion

        return result;
    }
}
