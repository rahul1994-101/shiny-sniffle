namespace Application.Features.Workspace.Tags.Queries;

using Application.Features.Workspace.Tags;
using FluentValidation;

public sealed record GetAllTagsByUserIdRequest(Guid UserId, bool IncludeInactive = false) : IQuery<GetAllTagsByUserIdResponse>;

public sealed class GetAllTagsByUserIdResponse
{
    public IReadOnlyList<TagDto> Tags { get; init; } = [];
}

public sealed class GetAllTagsByUserIdRequestValidator : AbstractValidator<GetAllTagsByUserIdRequest>
{
    public GetAllTagsByUserIdRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
    }
}

public sealed class GetAllTagsByUserIdRequestHandler(TagRepository tagRepo)
    : IRequestHandler<GetAllTagsByUserIdRequest, GetAllTagsByUserIdResponse>
{
    public async ValueTask<Result<GetAllTagsByUserIdResponse>> HandleAsync(
        GetAllTagsByUserIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<GetAllTagsByUserIdResponse>();

        #region # Execute

        var tags = await tagRepo.GetAllTagsByUserIdAsync(request.UserId, request.IncludeInactive, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new GetAllTagsByUserIdResponse { Tags = tags });

        #endregion

        return result;
    }
}
