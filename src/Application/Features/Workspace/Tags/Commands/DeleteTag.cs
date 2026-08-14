namespace Application.Features.Workspace.Tags.Commands;

using Application.Features.Workspace.Tags;
using FluentValidation;

public sealed record DeleteTagRequest(Guid UserId, Guid TagId) : ICommand<DeleteTagResponse>;

public sealed class DeleteTagResponse;

public sealed class DeleteTagRequestValidator : AbstractValidator<DeleteTagRequest>
{
    public DeleteTagRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.TagId).NotEmpty().WithMessage("Tag Id is required.");
    }
}

public sealed class DeleteTagRequestHandler(TagRepository tagRepo)
    : IRequestHandler<DeleteTagRequest, DeleteTagResponse>
{
    public async ValueTask<Result<DeleteTagResponse>> HandleAsync(
        DeleteTagRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<DeleteTagResponse>();

        #region # Execute

        var deleted = await tagRepo.SoftDeleteAsync(request.UserId, request.TagId, request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        if (!deleted)
        {
            result.Failure(ErrorCode.NotFound, "Tag not found.");
        }
        else
        {
            result.Success(new DeleteTagResponse());
        }

        #endregion

        return result;
    }
}
