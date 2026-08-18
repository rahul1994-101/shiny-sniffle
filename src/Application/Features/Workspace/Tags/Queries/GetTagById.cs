namespace Application.Features.Workspace.Tags.Queries;

using Application.Features.Workspace.Tags;
using FluentValidation;

public sealed record GetTagByIdRequest(Guid UserId, Guid TagId) : IQuery<GetTagByIdResponse>;

public sealed class GetTagByIdResponse : TagDto
{
}

public sealed class GetTagByIdRequestValidator : AbstractValidator<GetTagByIdRequest>
{
    public GetTagByIdRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.TagId).NotEmpty().WithMessage("Tag Id is required.");
    }
}

public sealed class GetTagByIdRequestHandler(TagRepository tagRepo)
    : IRequestHandler<GetTagByIdRequest, GetTagByIdResponse>
{
    public async ValueTask<Result<GetTagByIdResponse>> HandleAsync(
        GetTagByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<GetTagByIdResponse>();

        #region # Execute

        var tag = await tagRepo.GetTagByIdAsync(request.UserId, request.TagId, cancellationToken);

        #endregion

        #region # Handle Result

        if (tag is null)
        {
            result.Failure(ErrorCode.NotFound, "Tag not found.");
        }
        else
        {
            result.Success(tag.AsResponse<GetTagByIdResponse>());
        }

        #endregion

        return result;
    }
}
