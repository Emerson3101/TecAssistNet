using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TecAssist.Application.Abstractions;
using TecAssist.Application.Common;
using TecAssist.Application.Contracts;
using TecAssist.Application.Options;
using TecAssist.Application.Rag;
using TecAssist.Domain.Conversations;
using TecAssist.UnitTests.Fakes;
using TecAssist.UnitTests.Persistence;

namespace TecAssist.UnitTests.Rag;

public sealed class ChatServiceTests : DatabaseTestBase
{
    private readonly Guid _userId = Guid.NewGuid();

    private async Task<(ChatService ChatService, TestDbContext Context)> CreateServiceAsync(
        IChunkSearcher chunkSearcher,
        IChatClient chatClient)
    {
        var context = await CreateContextAsync();
        var currentUser = new FakeCurrentUser(_userId);
        var service = new ChatService(
            context,
            currentUser,
            chunkSearcher,
            chatClient,
            new PromptBuilder(),
            Options.Create(new RagOptions()));
        return (service, context);
    }

    private async Task<Conversation> SeedConversationAsync(TestDbContext context, string? title = null)
    {
        var conversation = new Conversation { UserId = _userId, Title = title };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();
        return conversation;
    }

    [Fact]
    public async Task StartMessageStream_ThrowsNotFound_ForMissingConversation()
    {
        var (service, _) = await CreateServiceAsync(
            new FakeChunkSearcher(FakeChunkSearcher.SingleResult()),
            FakeChatClient.RespondingWith("answer"));
        var missingId = Guid.NewGuid();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.StartMessageStreamAsync(missingId, "hello"));
    }

    [Fact]
    public async Task StartMessageStream_ThrowsNotFound_ForForeignConversation()
    {
        var (service, context) = await CreateServiceAsync(
            new FakeChunkSearcher(FakeChunkSearcher.SingleResult()),
            FakeChatClient.RespondingWith("answer"));
        var foreign = new Conversation { UserId = Guid.NewGuid() };
        context.Conversations.Add(foreign);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.StartMessageStreamAsync(foreign.Id, "hello"));
    }

    [Fact]
    public async Task SendMessage_StreamsTokens_Citations_AndCompleted()
    {
        var contextResults = FakeChunkSearcher.SingleResult();
        var chatClient = new FakeChatClient(["The answer", " is 90 days", " [1]."]);
        var (service, context) = await CreateServiceAsync(new FakeChunkSearcher(contextResults), chatClient);
        var conversation = await SeedConversationAsync(context);

        var stream = await service.StartMessageStreamAsync(conversation.Id, "How often to calibrate?");
        var events = new List<ChatStreamEvent>();
        await foreach (var streamEvent in stream.Events)
        {
            events.Add(streamEvent);
        }

        Assert.Equal(3, events.OfType<TokenEvent>().Count());
        var citations = Assert.Single(events.OfType<CitationsEvent>());
        Assert.Single(citations.Citations);
        var completed = Assert.Single(events.OfType<CompletedEvent>());

        var messages = await context.Messages.ToListAsync(CancellationToken.None);
        Assert.Equal(2, messages.Count);

        var userMessage = messages.Single(message => message.Role == MessageRole.User);
        Assert.Equal(completed.UserMessageId, userMessage.Id);

        var assistantMessage = messages.Single(message => message.Role == MessageRole.Assistant);
        Assert.Equal(completed.AssistantMessageId, assistantMessage.Id);
        Assert.Equal("The answer is 90 days [1].", assistantMessage.Content);
        Assert.Single(assistantMessage.Citations);
    }

    [Fact]
    public async Task SendMessage_LeavesConversationUntitled()
    {
        var (service, context) = await CreateServiceAsync(
            new FakeChunkSearcher(FakeChunkSearcher.SingleResult()),
            FakeChatClient.RespondingWith("short answer"));
        var conversation = await SeedConversationAsync(context);

        await using var enumerator = (await service.StartMessageStreamAsync(conversation.Id, "Explain the calibration schedule"))
            .Events.GetAsyncEnumerator();
        while (await enumerator.MoveNextAsync()) { }

        await context.Entry(conversation).ReloadAsync();
        Assert.Null(conversation.Title);
    }

    [Fact]
    public async Task GenerateTitle_NamesConversation_FromFirstExchange()
    {
        var (service, context) = await CreateServiceAsync(
            new FakeChunkSearcher(FakeChunkSearcher.SingleResult()),
            new FakeChatClient(["Calibration", " interval"]));
        var conversation = await SeedConversationAsync(context);

        await using var enumerator = (await service.StartMessageStreamAsync(conversation.Id, "How often to calibrate?"))
            .Events.GetAsyncEnumerator();
        while (await enumerator.MoveNextAsync()) { }

        var title = await service.GenerateTitleAsync(conversation.Id);

        await context.Entry(conversation).ReloadAsync();
        Assert.Equal("Calibration interval", title);
        Assert.Equal("Calibration interval", conversation.Title);
    }

    [Fact]
    public async Task GenerateTitle_CleansModelOutput()
    {
        var (service, context) = await CreateServiceAsync(
            new FakeChunkSearcher(FakeChunkSearcher.SingleResult()),
            new FakeChatClient(["  \"Voltage compliance", "\n Q3\".  "]));
        var conversation = await SeedConversationAsync(context);

        await using var enumerator = (await service.StartMessageStreamAsync(conversation.Id, "question"))
            .Events.GetAsyncEnumerator();
        while (await enumerator.MoveNextAsync()) { }

        var title = await service.GenerateTitleAsync(conversation.Id);

        Assert.Equal("Voltage compliance Q3", title);
    }

    [Fact]
    public async Task GenerateTitle_IsNoOp_WhenTitleExists()
    {
        var (service, context) = await CreateServiceAsync(
            new FakeChunkSearcher(FakeChunkSearcher.SingleResult()),
            new FakeChatClient(["Generated", " title"]));
        var conversation = await SeedConversationAsync(context, "Custom title");

        var title = await service.GenerateTitleAsync(conversation.Id);

        await context.Entry(conversation).ReloadAsync();
        Assert.Equal("Custom title", title);
    }

    [Fact]
    public async Task GenerateTitle_LeavesTitleNull_WhenNoMessages()
    {
        var (service, context) = await CreateServiceAsync(
            new FakeChunkSearcher(FakeChunkSearcher.SingleResult()),
            FakeChatClient.RespondingWith("title"));
        var conversation = await SeedConversationAsync(context);

        var title = await service.GenerateTitleAsync(conversation.Id);

        Assert.Null(title);
    }

    [Fact]
    public async Task GenerateTitle_ThrowsNotFound_ForMissingConversation()
    {
        var (service, _) = await CreateServiceAsync(
            new FakeChunkSearcher(FakeChunkSearcher.SingleResult()),
            FakeChatClient.RespondingWith("title"));

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GenerateTitleAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SendMessage_PromptIncludesContext_Excerpt()
    {
        var contextResults = FakeChunkSearcher.SingleResult();
        var chatClient = new FakeChatClient(["ok"]);
        var (service, context) = await CreateServiceAsync(new FakeChunkSearcher(contextResults), chatClient);
        var conversation = await SeedConversationAsync(context);

        var stream = await service.StartMessageStreamAsync(conversation.Id, "question");
        await foreach (var _ in stream.Events) { }

        var prompt = Assert.Single(chatClient.ReceivedPrompts);
        Assert.Contains("The maintenance interval is 90 days.", prompt[0].Content);
        Assert.Equal("question", prompt[^1].Content);
    }

    [Fact]
    public async Task SendMessage_ChatFailure_ThrowsChatGenerationException()
    {
        var failing = new FailingChatClient();
        var (service, context) = await CreateServiceAsync(
            new FakeChunkSearcher(FakeChunkSearcher.SingleResult()),
            failing);
        var conversation = await SeedConversationAsync(context);

        var stream = await service.StartMessageStreamAsync(conversation.Id, "question");
        await Assert.ThrowsAsync<ChatGenerationException>(async () =>
        {
            await foreach (var _ in stream.Events) { }
        });
    }

    [Fact]
    public async Task SendMessage_EmptyAnswer_ThrowsChatGenerationException_AndPersistsNoAssistantMessage()
    {
        var (service, context) = await CreateServiceAsync(
            new FakeChunkSearcher(FakeChunkSearcher.SingleResult()),
            new FakeChatClient([]));
        var conversation = await SeedConversationAsync(context);

        var stream = await service.StartMessageStreamAsync(conversation.Id, "question");
        await Assert.ThrowsAsync<ChatGenerationException>(async () =>
        {
            await foreach (var _ in stream.Events) { }
        });

        var messages = await context.Messages.ToListAsync(CancellationToken.None);
        var userMessage = Assert.Single(messages);
        Assert.Equal(MessageRole.User, userMessage.Role);
        Assert.Empty(userMessage.Citations);
    }

    private sealed class FailingChatClient : IChatClient
    {
        public async IAsyncEnumerable<string> StreamCompletionAsync(
            IReadOnlyList<ChatMessage> messages,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return string.Empty;
            await Task.Yield();
            throw new InvalidOperationException("provider is down");
        }
    }
}
