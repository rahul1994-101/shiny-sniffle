namespace Application.Features.Workspace.Buckets.Queries;

using Application.Features.Workspace.Buckets;
using FluentValidation;

public sealed record GetAllBucketsByUserIdRequest(Guid UserId, bool IncludeInactive = false) : IQuery<GetAllBucketsByUserIdResponse>;

public sealed class GetAllBucketsByUserIdResponse
{
    public IReadOnlyList<BucketDto> Buckets { get; init; } = [];
}

public sealed class GetAllBucketsByUserIdRequestValidator : AbstractValidator<GetAllBucketsByUserIdRequest>
{
    public GetAllBucketsByUserIdRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
    }
}

public sealed class GetAllBucketsByUserIdRequestHandler(BucketRepository bucketRepo)
    : IRequestHandler<GetAllBucketsByUserIdRequest, GetAllBucketsByUserIdResponse>
{
    public async ValueTask<Result<GetAllBucketsByUserIdResponse>> HandleAsync(
        GetAllBucketsByUserIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<GetAllBucketsByUserIdResponse>();

        #region # Execute

        var buckets = await bucketRepo.GetAllBucketsByUserIdAsync(request.UserId, request.IncludeInactive, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new GetAllBucketsByUserIdResponse { Buckets = buckets });

        #endregion

        return result;
    }
}
