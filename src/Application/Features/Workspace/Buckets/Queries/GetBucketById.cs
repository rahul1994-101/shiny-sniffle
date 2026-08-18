namespace Application.Features.Workspace.Buckets.Queries;

using Application.Features.Workspace.Buckets;
using FluentValidation;

public sealed record GetBucketByIdRequest(Guid UserId, Guid BucketId) : IQuery<GetBucketByIdResponse>;

public sealed class GetBucketByIdResponse : BucketDto
{
}

public sealed class GetBucketByIdRequestValidator : AbstractValidator<GetBucketByIdRequest>
{
    public GetBucketByIdRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.BucketId).NotEmpty().WithMessage("Bucket Id is required.");
    }
}

public sealed class GetBucketByIdRequestHandler(BucketRepository bucketRepo)
    : IRequestHandler<GetBucketByIdRequest, GetBucketByIdResponse>
{
    public async ValueTask<Result<GetBucketByIdResponse>> HandleAsync(
        GetBucketByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<GetBucketByIdResponse>();

        #region # Execute

        var bucket = await bucketRepo.GetBucketByIdAsync(request.UserId, request.BucketId, cancellationToken);

        #endregion

        #region # Handle Result

        if (bucket is null)
        {
            result.Failure(ErrorCode.NotFound, "Bucket not found.");
        }
        else
        {
            result.Success(bucket.AsResponse<GetBucketByIdResponse>());
        }

        #endregion

        return result;
    }
}
