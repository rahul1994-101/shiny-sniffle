using FluentValidation;

using WebApp.AI;
using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatMessage.Commands;

public sealed record SendChatMessageRequest(
    Guid ChatThreadId,
    Guid UserId,
    ChatAgent ChatAgent,
    string Message)
    : ICommand<AppResult<SendChatMessageResponse?>>;

public sealed class SendChatMessageRequestValidator : AbstractValidator<SendChatMessageRequest>
{
    public SendChatMessageRequestValidator()
    {
        RuleFor(x => x.ChatThreadId)
            .NotEmpty()
            .WithMessage("Chat Thread Id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.Message)
            .Must(message => !string.IsNullOrWhiteSpace(message))
            .WithMessage("Message is required.");
    }
}

public sealed class SendChatMessageRequestHandler(
    IChatThreadRepository chatThreads,
    IChatMessageRepository chatMessages,
    ChatOrchestrator chatOrchestrator)
    : IFeatureHandler<SendChatMessageRequest, AppResult<SendChatMessageResponse?>>
{
    public async Task<AppResult<SendChatMessageResponse?>> HandleAsync(
        SendChatMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<SendChatMessageResponse?>();
        var text = request.Message.Trim();

        var thread = await chatThreads.GetActiveByIdAsync(request.ChatThreadId, cancellationToken);
        if (thread is null || thread.UserId != request.UserId)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
            return result;
        }

        var chatAgent = request.ChatAgent == thread.ChatAgent
            ? request.ChatAgent
            : thread.ChatAgent;

        #region # Execute

        var userMessage = await chatMessages.AddAsync(new Core.Entities.ChatMessage
        {
            ChatThreadId = request.ChatThreadId,
            Role = ChatMessageRoles.User,
            Content = text,
            CreatedBy = request.UserId,
            UpdatedBy = request.UserId
        }, cancellationToken);
        if (userMessage is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to create chat message.");
            return result;
        }

        var agentRun = await chatOrchestrator.RunChatAgentAsync(new RunChatAgentRequest
        {
            ChatThreadId = request.ChatThreadId,
            UserId = request.UserId,
            ChatAgent = chatAgent
        });

        var assistantMessage = await chatMessages.AddAsync(new Core.Entities.ChatMessage
        {
            ChatThreadId = request.ChatThreadId,
            Role = ChatMessageRoles.Assistant,
            Content = agentRun.AssistantContent,
            CreatedBy = request.UserId,
            UpdatedBy = request.UserId
        }, cancellationToken);
        if (assistantMessage is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to create chat message.");
            return result;
        }

        #endregion

        #region # Handle Result

        result.Success(new SendChatMessageResponse
        {
            UserMessage = ChatMessageResponse.FromEntity(userMessage),
            AssistantMessage = ChatMessageResponse.FromEntity(assistantMessage)
        });

        #endregion

        return result;
    }
}

public sealed class SendChatMessageResponse
{
    public ChatMessageResponse UserMessage { get; init; } = null!;
    public ChatMessageResponse AssistantMessage { get; init; } = null!;
}
