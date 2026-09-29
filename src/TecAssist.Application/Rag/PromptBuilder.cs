using System.Text;
using TecAssist.Application.Abstractions;
using TecAssist.Domain.Conversations;

namespace TecAssist.Application.Rag;

public sealed class PromptBuilder
{
    private const string SystemPromptTemplate = """
        You are TecAssist, a technical documentation assistant.
        Answer the user's questions using ONLY the numbered context excerpts provided below.

        Rules:
        - Ground every factual statement in the context excerpts.
        - Cite the excerpt numbers you used inline, for example [1] or [2][3].
        - If the context does not contain the answer, say so plainly and do not guess.
        - Prefer concise, technically precise answers formatted in Markdown.
        """;

    public IReadOnlyList<ChatMessage> Build(
        IReadOnlyList<ChunkSearchResult> context,
        IReadOnlyList<Message> history,
        string question)
    {
        var prompt = new List<ChatMessage>
        {
            new(ChatRoles.System, BuildSystemPrompt(context))
        };

        prompt.AddRange(history.Select(message => new ChatMessage(
            message.Role == MessageRole.User ? ChatRoles.User : ChatRoles.Assistant,
            message.Content)));

        prompt.Add(new ChatMessage(ChatRoles.User, question));
        return prompt;
    }

    private static string BuildSystemPrompt(IReadOnlyList<ChunkSearchResult> context)
    {
        if (context.Count == 0)
        {
            return SystemPromptTemplate + "\n\nContext: (no indexed documents matched this question)";
        }

        var builder = new StringBuilder(SystemPromptTemplate);
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("Context excerpts:");

        for (var index = 0; index < context.Count; index++)
        {
            builder.AppendLine();
            builder.Append('[').Append(index + 1).Append("] From \"").Append(context[index].DocumentTitle).AppendLine("\":");
            builder.AppendLine(context[index].Content);
        }

        return builder.ToString();
    }
}
