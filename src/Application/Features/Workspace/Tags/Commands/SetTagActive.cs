namespace Application.Features.Workspace.Tags.Commands;

using Application.Features.Workspace.Tags;
using FluentValidation;

public sealed record SetTagActiveRequest(Guid UserId, Guid TagId, bool IsActive) : ICommand<SetTagActiveResponse>;

public sealed class SetTagActiveResponse : TagDto
{
}

public sealed class SetTagActiveRequestValidator : AbstractValidator<SetTagActiveRequest>
{
    public SetTagActiveRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.TagId).NotEmpty().WithMessage("Tag Id is required.");
    }
}

public sealed class SetTagActiveRequestHandler(TagRepository tagRepo)
    : IRequestHandler<SetTagActiveRequest, SetTagActiveResponse>
{
    public async ValueTask<Result<SetTagActiveResponse>> HandleAsync(
        SetTagActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<SetTagActiveResponse>();

        #region # Execute

        var updated = await tagRepo.SetActiveAsync(
            request.UserId,
            request.TagId,
            request.IsActive,
            request.UserId,
            cancellationToken);

        TagDto? tag = null;
        if (updated)
        {
            tag = await tagRepo.GetTagByIdAsync(request.UserId, request.TagId, cancellationToken);
        }

        #endregion

        #region # Handle Result

        if (!updated || tag is null)
        {
            result.Failure(ErrorCode.NotFound, "Tag not found.");
        }
        else
        {
            result.Success(tag.AsResponse<SetTagActiveResponse>());
        }

        #endregion

        return result;
    }
}
