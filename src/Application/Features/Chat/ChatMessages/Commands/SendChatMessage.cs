using FluentValidation;
using Application.AI;
using Application.AI.Memory;
using Application.Features.Chat.ChatThreads;
using Application.Features.Shared;

namespace Application.Features.Chat.ChatMessages.Commands;

public sealed record SendChatMessageRequest(Guid UserId, Guid ThreadId, string Message)
    : ICommand<SendChatMessageResponse>;

public sealed class SendChatMessageResponse
{
    public ChatMessageDto UserMessage { get; init; } = null!;
    public ChatMessageDto AssistantMessage { get; init; } = null!;
}

public sealed class SendChatMessageRequestValidator : AbstractValidator<SendChatMessageRequest>
{
    public SendChatMessageRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.ThreadId)
            .NotEmpty()
            .WithMessage("Thread Id is required.");

        RuleFor(x => x.Message)
            .Must(message => !string.IsNullOrWhiteSpace(message))
            .WithMessage("Message is required.");
    }
}

public sealed class SendChatMessageRequestHandler(
    ChatThreadRepository chatThreadRepo,
    ChatMessageRepository chatMessageRepo,
    ChatOrchestrator chatOrchestrator,
    ThreadMemoryService threadMemory,
    EntityRefMentionContextService mentionContextService)
    : IRequestHandler<SendChatMessageRequest, SendChatMessageResponse>
{
    public async ValueTask<Result<SendChatMessageResponse>> HandleAsync(SendChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<SendChatMessageResponse>();
        var text = request.Message.Trim();

        #region # Execute

        var thread = await chatThreadRepo.GetChatThreadByIdAsync(request.UserId, request.ThreadId, cancellationToken);
        SendChatMessageResponse? response = null;

        if (thread is not null)
        {
            var userMessage = await chatMessageRepo.AddAsync(new ChatMessage
            {
                ChatThreadId = request.ThreadId,
                Role = ChatMessageRoles.User,
                Content = text
            }, cancellationToken);

            var mentionContext = await mentionContextService.BuildContextBlockAsync(
                request.UserId,
                text,
                cancellationToken);

            var agentRun = await chatOrchestrator.RunChatAgentAsync(new RunChatAgentRequest
            {
                UserId = request.UserId,
                ThreadId = request.ThreadId,
                ChatAgent = thread.ChatAgent,
                MentionContext = mentionContext
            }, cancellationToken);

            var assistantMessage = await chatMessageRepo.AddAsync(new ChatMessage
            {
                ChatThreadId = request.ThreadId,
                Role = ChatMessageRoles.Assistant,
                Content = agentRun.AssistantContent
            }, cancellationToken);

            await threadMemory.RefreshAsync(request.UserId, request.ThreadId, cancellationToken);
            response = new SendChatMessageResponse { UserMessage = userMessage, AssistantMessage = assistantMessage };
        }

        #endregion

        #region # Handle Result

        if (thread is null)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else
        {
            result.Success(response!);
        }

        #endregion

        return result;
    }
}
