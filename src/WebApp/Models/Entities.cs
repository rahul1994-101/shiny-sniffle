using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

#region # Base Entities

public abstract class BaseEntity
{
    // Primary key with auto-generated sequential UUID
    public Guid Id { get; set; }

    // Status and lifecycle management
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
}

public abstract class BaseAuditableEntity : BaseEntity
{
    // Audit fields for tracking changes
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region # User

public class User : BaseAuditableEntity
{
    //private string _password;

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [StringLength(255, MinimumLength = 5, ErrorMessage = "Email must be between 5 and 255 characters.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20, MinimumLength = 0, ErrorMessage = "Mobile must be between 0 and 20 characters.")]
    public string? Mobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 255 characters.")]
    public string Password { get; set; } = string.Empty;
    //public string Password
    //{
    //    get => _password.Decrypt();
    //    set => _password = value.Encrypt();
    //}
}

#endregion

#region # ChatThread

public class ChatThread : BaseAuditableEntity
{
    [Required(ErrorMessage = "UserId is required.")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    public ChatAgent ChatAgent { get; set; }
}

#endregion

#region # ChatMessage

public class ChatMessage : BaseAuditableEntity
{
    [Required(ErrorMessage = "Chat Thread Id is required.")]
    public Guid ChatThreadId { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "Role must be between 1 and 20 characters.")]
    public string Role { get; set; } = ChatMessageRoles.User;

    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; set; } = string.Empty;
}

#endregion

#region # UserSetting

public class UserSetting : BaseAuditableEntity
{
    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }

    /// <summary>JSON payload for <see cref="EmailSettings"/>; column <c>EmailSettingsJson</c>.</summary>
    public string? EmailSettingsJson { get; set; }
}

public class EmailSettings
{
    public string EmailAddress { get; set; } = string.Empty;

    public string ImapHost { get; set; } = string.Empty;

    public int ImapPort { get; set; } = 993;

    public bool ImapUseSsl { get; set; } = true;

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool SmtpUseSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

#endregion
