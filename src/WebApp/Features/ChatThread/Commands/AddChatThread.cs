using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatThread.Commands;

public sealed record AddChatThreadRequest(string Title, Guid UserId, ChatAgent ChatAgent = default)
    : ICommand<AppResult<ChatThreadResponse?>>;

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

public sealed class AddChatThreadRequestHandler(IChatThreadRepository chatThreads)
    : IFeatureHandler<AddChatThreadRequest, AppResult<ChatThreadResponse?>>
{
    public async Task<AppResult<ChatThreadResponse?>> HandleAsync(
        AddChatThreadRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<ChatThreadResponse?>();

        #region # Execute

        var entity = new Core.Entities.ChatThread
        {
            Title = request.Title,
            UserId = request.UserId,
            ChatAgent = request.ChatAgent,
            CreatedBy = request.UserId,
            UpdatedBy = request.UserId
        };

        var chatThread = await chatThreads.AddAsync(entity, cancellationToken);

        #endregion

        #region # Handle Result

        if (chatThread is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to create chat thread.");
        }
        else
        {
            result.Success(ChatThreadResponse.FromEntity(chatThread));
        }

        #endregion

        return result;
    }
}
