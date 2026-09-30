using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TecAssist.IntegrationTests.Fakes;
using TecAssist.IntegrationTests.Infrastructure;

namespace TecAssist.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ChatWorkflowTests(ApiFactory factory)
{
    private static async Task<Guid> CreateConversationAsync(HttpClient client, string title = "Test conversation")
    {
        var response = await client.PostAsJsonAsync("/api/conversations", new { title });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var id = payload.GetProperty("id").GetGuid();
        Assert.Equal(title, payload.GetProperty("title").GetString());
        return id;
    }

    private static async Task<JsonElement> UploadDocumentAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/documents/text",
            new
            {
                title = "Calibration Guide",
                content = "The maintenance interval is 90 days. Regular calibration is required.",
                sourceType = "text",
            });
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    private static async Task<JsonElement> WaitForDocumentReadyAsync(HttpClient client, Guid id)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/api/documents/{id}");
            response.EnsureSuccessStatusCode();
            var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            if (payload.GetProperty("status").GetString() == "ready")
            {
                return payload;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("Document never became ready.");
    }

    private static async Task CleanupAllDocumentsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/documents");
        response.EnsureSuccessStatusCode();
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        foreach (var document in payload.EnumerateArray())
        {
            var deleteResponse = await client.DeleteAsync($"/api/documents/{document.GetProperty("id").GetGuid()}");
            deleteResponse.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task SendMessage_StreamsSseFrames_TokensCitationsDone()
    {
        using var client = factory.CreateClient();
        await CleanupAllDocumentsAsync(client);
        var documentId = (await UploadDocumentAsync(client)).GetProperty("id").GetGuid();
        await WaitForDocumentReadyAsync(client, documentId);

        var conversationId = await CreateConversationAsync(client);
        var response = await client.PostAsJsonAsync(
            $"/api/conversations/{conversationId}/messages",
            new { content = "How often should we calibrate?" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith(
            "text/event-stream",
            response.Content.Headers.ContentType?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        var frames = SseFrame.Parse(await response.Content.ReadAsStringAsync());

        Assert.NotEmpty(frames);
        Assert.DoesNotContain(frames, frame => frame.Event == "error");

        var events = frames.Select(frame => frame.Event).Distinct().ToArray();
        Assert.Contains("token", events);
        Assert.Contains("citation", events);
        Assert.Contains("done", events);

        var streamed = string.Concat(
            frames
                .Where(frame => frame.Event == "token")
                .Select(frame => frame.Data.GetProperty("text").GetString()));
        Assert.Equal(FakeStreamingChatClient.ExpectedAnswer, streamed);

        Assert.Equal("done", frames[^1].Event);
        Assert.NotEqual(Guid.Empty, frames[^1].Data.GetProperty("userMessageId").GetGuid());
        Assert.NotEqual(Guid.Empty, frames[^1].Data.GetProperty("assistantMessageId").GetGuid());

        var citationFrames = frames.Where(frame => frame.Event == "citation").ToArray();
        Assert.All(citationFrames, frame =>
        {
            Assert.Equal("Calibration Guide", frame.Data.GetProperty("documentTitle").GetString());
            Assert.NotEqual(Guid.Empty, frame.Data.GetProperty("chunkId").GetGuid());
        });
    }

    [Fact]
    public async Task SendMessage_PersistsHistory_AndCitations()
    {
        using var client = factory.CreateClient();
        await CleanupAllDocumentsAsync(client);
        var documentId = (await UploadDocumentAsync(client)).GetProperty("id").GetGuid();
        await WaitForDocumentReadyAsync(client, documentId);

        var conversationId = await CreateConversationAsync(client);
        var response = await client.PostAsJsonAsync(
            $"/api/conversations/{conversationId}/messages",
            new { content = "Calibration question" });
        response.EnsureSuccessStatusCode();
        await response.Content.ReadAsStringAsync();

        var historyResponse = await client.GetAsync($"/api/conversations/{conversationId}/messages");
        historyResponse.EnsureSuccessStatusCode();

        var history = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(2, history.GetArrayLength());

        var assistant = history[1];
        Assert.Equal("assistant", assistant.GetProperty("role").GetString());
        Assert.Equal(FakeStreamingChatClient.ExpectedAnswer, assistant.GetProperty("content").GetString());
        Assert.True(assistant.GetProperty("citations").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task SendMessage_UnknownConversation_Returns404()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/api/conversations/{Guid.NewGuid()}/messages",
            new { content = "hello" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GenerateTitle_NamesConversation_AfterFirstExchange()
    {
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/conversations", new { });
        createResponse.EnsureSuccessStatusCode();
        var conversationId = JsonDocument
            .Parse(await createResponse.Content.ReadAsStringAsync())
            .RootElement
            .GetProperty("id")
            .GetGuid();

        var sendResponse = await client.PostAsJsonAsync(
            $"/api/conversations/{conversationId}/messages",
            new { content = "How often should we calibrate?" });
        sendResponse.EnsureSuccessStatusCode();
        await sendResponse.Content.ReadAsStringAsync();

        var titleResponse = await client.PostAsync($"/api/conversations/{conversationId}/title", null);
        titleResponse.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await titleResponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(conversationId, payload.GetProperty("id").GetGuid());
        Assert.Equal(
            "The maintenance interval is 90 days [1]",
            payload.GetProperty("title").GetString());

        var listResponse = await client.GetAsync("/api/conversations");
        listResponse.EnsureSuccessStatusCode();
        var list = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync()).RootElement;
        var listed = list
            .EnumerateArray()
            .Single(conversation => conversation.GetProperty("id").GetGuid() == conversationId);
        Assert.Equal(
            "The maintenance interval is 90 days [1]",
            listed.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GenerateTitle_KeepsCustomTitle()
    {
        using var client = factory.CreateClient();
        var conversationId = await CreateConversationAsync(client, "Custom title");

        var titleResponse = await client.PostAsync($"/api/conversations/{conversationId}/title", null);
        titleResponse.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await titleResponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Custom title", payload.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Conversation_CanBeRenamedAndDeleted()
    {
        using var client = factory.CreateClient();
        var conversationId = await CreateConversationAsync(client, "Before rename");

        var renameResponse = await client.PatchAsJsonAsync(
            $"/api/conversations/{conversationId}",
            new { title = "After rename" });
        renameResponse.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await renameResponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("After rename", payload.GetProperty("title").GetString());

        var deleteResponse = await client.DeleteAsync($"/api/conversations/{conversationId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var messagesResponse = await client.GetAsync($"/api/conversations/{conversationId}/messages");
        Assert.Equal(HttpStatusCode.NotFound, messagesResponse.StatusCode);
    }
}
