namespace Application.Features.Workspace.Buckets.Commands;

using Application.Features.Workspace.Buckets;
using FluentValidation;

public sealed record SetBucketActiveRequest(Guid UserId, Guid BucketId, bool IsActive) : ICommand<SetBucketActiveResponse>;

public sealed class SetBucketActiveResponse : BucketDto
{
}

public sealed class SetBucketActiveRequestValidator : AbstractValidator<SetBucketActiveRequest>
{
    public SetBucketActiveRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.BucketId).NotEmpty().WithMessage("Bucket Id is required.");
    }
}

public sealed class SetBucketActiveRequestHandler(BucketRepository bucketRepo)
    : IRequestHandler<SetBucketActiveRequest, SetBucketActiveResponse>
{
    public async ValueTask<Result<SetBucketActiveResponse>> HandleAsync(
        SetBucketActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<SetBucketActiveResponse>();

        #region # Execute

        var updated = await bucketRepo.SetActiveAsync(
            request.UserId,
            request.BucketId,
            request.IsActive,
            request.UserId,
            cancellationToken);

        BucketDto? bucket = null;
        if (updated)
        {
            bucket = await bucketRepo.GetBucketByIdAsync(request.UserId, request.BucketId, cancellationToken);
        }

        #endregion

        #region # Handle Result

        if (!updated || bucket is null)
        {
            result.Failure(ErrorCode.NotFound, "Bucket not found.");
        }
        else
        {
            result.Success(bucket.AsResponse<SetBucketActiveResponse>());
        }

        #endregion

        return result;
    }
}
