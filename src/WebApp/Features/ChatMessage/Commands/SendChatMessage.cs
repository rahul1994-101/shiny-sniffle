using FluentValidation;

using WebApp.AI;
using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatMessage.Commands;

public sealed record SendChatMessageCommand(SendChatMessageRequest Request)
    : ICommand<AppResult<SendChatMessageResponse?>>;

public sealed class SendChatMessageCommandValidator : AbstractValidator<SendChatMessageCommand>
{
    public SendChatMessageCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request can't be empty.");

        RuleFor(x => x.Request.ChatThreadId)
            .NotEmpty()
            .WithMessage("Chat Thread Id is required.");

        RuleFor(x => x.Request.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.Request.Message)
            .Must(message => !string.IsNullOrWhiteSpace(message))
            .WithMessage("Message is required.");
    }
}

public sealed class SendChatMessageCommandHandler(
    IChatThreadRepository chatThreads,
    IChatMessageRepository chatMessages,
    ChatOrchestrator chatOrchestrator)
    : IFeatureHandler<SendChatMessageCommand, AppResult<SendChatMessageResponse?>>
{
    public async Task<AppResult<SendChatMessageResponse?>> HandleAsync(
        SendChatMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<SendChatMessageResponse?>();
        var request = command.Request;
        var text = request.Message.Trim();

        var thread = await chatThreads.GetChatThreadByIdAsync(request.ChatThreadId);
        if (thread is null || thread.UserId != request.UserId)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
            return result;
        }

        var chatAgent = request.ChatAgent == thread.ChatAgent
            ? request.ChatAgent
            : thread.ChatAgent;

        #region # Execute

        var userMessage = await chatMessages.AddChatMessageAsync(new Core.Entities.ChatMessage
        {
            ChatThreadId = request.ChatThreadId,
            Role = ChatMessageRoles.User,
            Content = text,
            CreatedBy = request.UserId,
            UpdatedBy = request.UserId
        });
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

        var assistantMessage = await chatMessages.AddChatMessageAsync(new Core.Entities.ChatMessage
        {
            ChatThreadId = request.ChatThreadId,
            Role = ChatMessageRoles.Assistant,
            Content = agentRun.AssistantContent,
            CreatedBy = request.UserId,
            UpdatedBy = request.UserId
        });
        if (assistantMessage is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to create chat message.");
            return result;
        }

        #endregion

        #region # Handle Result

        result.Success(new SendChatMessageResponse
        {
            UserMessage = userMessage,
            AssistantMessage = assistantMessage
        });

        #endregion

        return result;
    }
}
