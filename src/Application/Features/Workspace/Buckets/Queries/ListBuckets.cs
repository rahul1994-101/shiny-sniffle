namespace Application.Features.Workspace.Buckets.Queries;

using Application.Features.Workspace.Buckets;
using FluentValidation;

public sealed record ListBucketsRequest(Guid UserId) : IQuery<ListBucketsResponse>;

public sealed class ListBucketsResponse
{
    public IReadOnlyList<BucketDto> Buckets { get; init; } = [];
}

public sealed class ListBucketsRequestValidator : AbstractValidator<ListBucketsRequest>
{
    public ListBucketsRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
    }
}

public sealed class ListBucketsRequestHandler(BucketRepository bucketRepo)
    : IRequestHandler<ListBucketsRequest, ListBucketsResponse>
{
    public async ValueTask<Result<ListBucketsResponse>> HandleAsync(
        ListBucketsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<ListBucketsResponse>();

        #region # Execute

        var buckets = await bucketRepo.ListAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new ListBucketsResponse { Buckets = buckets });

        #endregion

        return result;
    }
}
