using FluentValidation;

using WebApp.Features.Shared.Cqrs.Abstractions;

namespace WebApp.Features.ChatThreads.Commands;

public sealed record DeleteChatThreadRequest(Guid Id, Guid UserId) : ICommand<AppResult>;

public sealed class DeleteChatThreadRequestValidator : AbstractValidator<DeleteChatThreadRequest>
{
    public DeleteChatThreadRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Thread Id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class DeleteChatThreadRequestHandler(ChatThreadRepository chatThreadRepo, SharedRepository sharedRepo)
    : IFeatureHandler<DeleteChatThreadRequest, AppResult>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;


    public async Task<AppResult> HandleAsync(DeleteChatThreadRequest request, CancellationToken cancellationToken = default)
    {
        var result = new AppResult();

        #region # Execute

        var deleted = await chatThreadRepo.DeleteAsync(request.Id, request.UserId, request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        if (!deleted)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else
        {
            result.Success();
        }

        #endregion

        return result;
    }
}
