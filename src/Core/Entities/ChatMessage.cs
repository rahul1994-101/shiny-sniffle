using System.ComponentModel.DataAnnotations;

namespace Core.Entities;

public class ChatMessage : BaseAuditableEntity
{
    [Required(ErrorMessage = "Chat Thread Id is required.")]
    public Guid ChatThreadId { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "Role must be between 1 and 20 characters.")]
    public string Role { get; set; } = "user";

    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; set; } = string.Empty;
}
