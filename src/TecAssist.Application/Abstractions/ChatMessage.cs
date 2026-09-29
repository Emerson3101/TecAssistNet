namespace TecAssist.Application.Abstractions;

public sealed record ChatMessage(string Role, string Content);

public static class ChatRoles
{
    public const string System = "system";

    public const string User = "user";

    public const string Assistant = "assistant";
}
