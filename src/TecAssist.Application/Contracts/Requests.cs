using System.ComponentModel.DataAnnotations;

namespace TecAssist.Application.Contracts;

public sealed class CreateConversationRequest
{
    [MaxLength(120)]
    public string? Title { get; set; }
}

public sealed class UpdateConversationRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;
}

public sealed class SendMessageRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(8000)]
    public string Content { get; set; } = string.Empty;
}

public sealed class CreateTextDocumentRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string Content { get; set; } = string.Empty;

    [AllowedValues("text", "markdown")]
    public string SourceType { get; set; } = "text";
}
