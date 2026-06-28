using FluentValidation;

using WebApp.Features._Shared.Abstractions;
using WebApp.Utilities.Services;

namespace WebApp.Features.ChatThread.Commands;

public sealed record UpdateChatThreadAgentRequest(Guid Id, Guid UserId, ChatAgent ChatAgent)
    : ICommand<AppResult<UpdateChatThreadAgentResponse?>>;

public sealed class UpdateChatThreadAgentRequestValidator : AbstractValidator<UpdateChatThreadAgentRequest>
{
    public UpdateChatThreadAgentRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Thread Id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class UpdateChatThreadAgentRequestHandler(
    IChatThreadRepository chatThreads,
    UserMailboxService mailboxService)
    : IFeatureHandler<UpdateChatThreadAgentRequest, AppResult<UpdateChatThreadAgentResponse?>>
{
    public async Task<AppResult<UpdateChatThreadAgentResponse?>> HandleAsync(
        UpdateChatThreadAgentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<UpdateChatThreadAgentResponse?>();

        if (request.ChatAgent == ChatAgent.Email
            && !await mailboxService.IsConfiguredAsync(request.UserId, cancellationToken))
        {
            result.Failure(
                ErrorCode.BadRequest,
                "Connect your mailbox in Settings → Email before using the Email agent.");
            return result;
        }

        #region # Execute

        var chatThread = await chatThreads.UpdateChatThreadAgentAsync(new Core.DTOs.UpdateChatThreadAgentRequest
        {
            Id = request.Id,
            UserId = request.UserId,
            ChatAgent = request.ChatAgent
        });

        #endregion

        #region # Handle Result

        if (chatThread is null)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else
        {
            result.Success(ToResponse(chatThread));
        }

        #endregion

        return result;
    }

    #region # Mapping

    private static UpdateChatThreadAgentResponse ToResponse(ChatThreadDto thread) => new()
    {
        Id = thread.Id,
        Title = thread.Title,
        UserId = thread.UserId,
        ChatAgent = thread.ChatAgent,
        CreatedAt = thread.CreatedAt,
        UpdatedAt = thread.UpdatedAt
    };

    #endregion
}

public sealed class UpdateChatThreadAgentResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public ChatAgent ChatAgent { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
