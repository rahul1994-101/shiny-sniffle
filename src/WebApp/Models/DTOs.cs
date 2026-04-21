namespace WebApp.Models;

public sealed class ChatMessageDTO
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public sealed class SendChatRequestDTO
{
    public string Message { get; set; } = string.Empty;
}

public sealed class SendChatResponseDTO
{
    public string Reply { get; set; } = string.Empty;
}
