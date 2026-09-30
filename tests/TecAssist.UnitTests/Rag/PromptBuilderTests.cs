using TecAssist.Application.Abstractions;
using TecAssist.Application.Rag;
using TecAssist.Domain.Conversations;

namespace TecAssist.UnitTests.Rag;

public sealed class PromptBuilderTests
{
    private static readonly IReadOnlyList<ChunkSearchResult> Context =
    [
        new ChunkSearchResult(Guid.NewGuid(), Guid.NewGuid(), "Manual.pdf", "Content of the first chunk.", 0.8),
        new ChunkSearchResult(Guid.NewGuid(), Guid.NewGuid(), "Report.pdf", "Content of the second chunk.", 0.6),
    ];

    private readonly PromptBuilder _builder = new();

    [Fact]
    public void Build_StartsWithSystemMessage_AndEndsWithUserQuestion()
    {
        var prompt = _builder.Build(Context, [], "What is the answer?");

        Assert.Equal("system", prompt[0].Role);
        var last = prompt[^1];
        Assert.Equal("user", last.Role);
        Assert.Equal("What is the answer?", last.Content);
    }

    [Fact]
    public void Build_IncludesNumberedContextExcerpts()
    {
        var prompt = _builder.Build(Context, [], "Question");

        var system = prompt[0].Content;
        Assert.Contains("[1] From \"Manual.pdf\"", system);
        Assert.Contains("Content of the first chunk.", system);
        Assert.Contains("[2] From \"Report.pdf\"", system);
        Assert.Contains("Content of the second chunk.", system);
    }

    [Fact]
    public void Build_IndicatesEmptyContext_WhenNoChunksMatch()
    {
        var prompt = _builder.Build([], [], "Question");

        Assert.Contains("no indexed documents matched", prompt[0].Content);
    }

    [Fact]
    public void Build_MapsConversationHistory_ToAlternatingRoles()
    {
        var history = new List<Message>
        {
            new() { Role = MessageRole.User, Content = "First question" },
            new() { Role = MessageRole.Assistant, Content = "First answer" },
        };

        var prompt = _builder.Build(Context, history, "Second question");

        Assert.Equal(4, prompt.Count);
        Assert.Equal("user", prompt[1].Role);
        Assert.Equal("First question", prompt[1].Content);
        Assert.Equal("assistant", prompt[2].Role);
        Assert.Equal("First answer", prompt[2].Content);
    }
}
