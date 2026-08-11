namespace Application.Features.workspace.Tags.Queries;

using Application.Features.workspace.Tags;
using FluentValidation;

public sealed record ListTagsRequest(Guid UserId) : IQuery<ListTagsResponse>;

public sealed class ListTagsResponse
{
    public IReadOnlyList<TagDto> Tags { get; init; } = [];
}

public sealed class ListTagsRequestValidator : AbstractValidator<ListTagsRequest>
{
    public ListTagsRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
    }
}

public sealed class ListTagsRequestHandler(TagRepository tagRepo)
    : IRequestHandler<ListTagsRequest, ListTagsResponse>
{
    public async ValueTask<Result<ListTagsResponse>> HandleAsync(
        ListTagsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<ListTagsResponse>();

        #region # Execute

        var tags = await tagRepo.ListAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new ListTagsResponse { Tags = tags });

        #endregion

        return result;
    }
}
