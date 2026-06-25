using System.ComponentModel.DataAnnotations;

using Core.DTOs;
using Core.Entities;

namespace WebApp.Models;

public class SendChatMessageRequest
{
    [Required(ErrorMessage = "Chat Thread Id is required.")]
    public Guid ChatThreadId { get; set; }

    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }

    public ChatAgent ChatAgent { get; set; }

    [Required(ErrorMessage = "Message is required.")]
    public string Message { get; set; } = string.Empty;
}

public class SendChatMessageResponse
{
    public ChatMessageDto UserMessage { get; set; } = null!;

    public ChatMessageDto AssistantMessage { get; set; } = null!;
}
