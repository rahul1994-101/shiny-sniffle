using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class ChatThreadDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public ChatAgent ChatAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class AddChatThreadRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }

    public ChatAgent ChatAgent { get; set; }
}

public class UpdateChatThreadAgentRequest
{
    [Required(ErrorMessage = "Thread Id is required.")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }

    public ChatAgent ChatAgent { get; set; }
}

public class UpdateChatThreadTitleRequest
{
    [Required(ErrorMessage = "Thread Id is required.")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }
}

public class DeleteChatThreadRequest
{
    [Required(ErrorMessage = "Thread Id is required.")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }
}
