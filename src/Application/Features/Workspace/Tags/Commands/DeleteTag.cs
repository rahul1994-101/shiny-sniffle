namespace Application.Features.workspace.Tags.Commands;

public sealed record DeleteTagRequest(Guid UserId, Guid TagId) : ICommand<DeleteTagResponse>;

public sealed class DeleteTagResponse;

public sealed class DeleteTagRequestHandler(TagRepository tagRepo)
    : IRequestHandler<DeleteTagRequest, DeleteTagResponse>
{
    public async ValueTask<Result<DeleteTagResponse>> HandleAsync(
        DeleteTagRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<DeleteTagResponse>();
        var deleted = await tagRepo.SoftDeleteAsync(request.UserId, request.TagId, request.UserId, cancellationToken);
        if (!deleted)
        {
            result.Failure(ErrorCode.NotFound, "Tag not found.");
        }
        else
        {
            result.Success(new DeleteTagResponse());
        }

        return result;
    }
}
