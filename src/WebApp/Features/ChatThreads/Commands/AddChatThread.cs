using FluentValidation;

using WebApp.Features.Shared.Cqrs.Abstractions;

namespace WebApp.Features.ChatThreads.Commands;

public sealed record AddChatThreadRequest(string Title, Guid UserId, ChatAgent ChatAgent = default)
    : ICommand<AppResult<AddChatThreadResponse?>>;

public sealed class AddChatThreadResponse : ChatThreadDto
{
}

public sealed class AddChatThreadRequestValidator : AbstractValidator<AddChatThreadRequest>
{
    public AddChatThreadRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .Length(1, 200)
            .WithMessage("Title must be between 1 and 200 characters.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class AddChatThreadRequestHandler(ChatThreadRepository chatThreadRepo, SharedRepository sharedRepo)
    : IFeatureHandler<AddChatThreadRequest, AppResult<AddChatThreadResponse?>>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;


    public async Task<AppResult<AddChatThreadResponse?>> HandleAsync(AddChatThreadRequest request, CancellationToken cancellationToken = default)
    {
        var result = new AppResult<AddChatThreadResponse?>();

        #region # Execute

        var entity = new ChatThread
        {
            Title = request.Title,
            UserId = request.UserId,
            ChatAgent = request.ChatAgent,
            CreatedBy = request.UserId,
            UpdatedBy = request.UserId
        };

        var chatThread = await chatThreadRepo.AddAsync(entity, cancellationToken);

        #endregion

        #region # Handle Result

        if (chatThread is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to create chat thread.");
        }
        else
        {
            result.Success(chatThread.AsResponse<AddChatThreadResponse>());
        }

        #endregion

        return result;
    }
}
