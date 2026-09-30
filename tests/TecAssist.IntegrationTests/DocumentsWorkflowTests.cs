using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TecAssist.IntegrationTests.Infrastructure;

namespace TecAssist.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class DocumentsWorkflowTests(ApiFactory factory)
{
    private async Task<(Guid Id, HttpClient Client)> UploadAndAcceptAsync(string title)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/documents/text",
            new { title, content = "Alpha beta gamma maintenance notes.", sourceType = "text" });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return (payload.GetProperty("id").GetGuid(), client);
    }

    private static async Task<JsonElement> WaitForStatusAsync(
        HttpClient client,
        Guid id,
        string expectedStatus,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/api/documents/{id}");
            response.EnsureSuccessStatusCode();
            var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

            if (payload.GetProperty("status").GetString() == expectedStatus)
            {
                return payload;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"Document {id} never reached status '{expectedStatus}'.");
    }

    [Fact]
    public async Task UploadText_IngestsToReady_WithChunks()
    {
        var (id, client) = await UploadAndAcceptAsync("Ingestion test report");

        var payload = await WaitForStatusAsync(client, id, "ready");

        Assert.Equal("Ingestion test report", payload.GetProperty("title").GetString());
        Assert.True(payload.GetProperty("chunkCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task Get_UnknownDocument_Returns404()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/documents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_IncludesUploadedDocuments()
    {
        var (id, _) = await UploadAndAcceptAsync("Listed document");

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/documents");
        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Contains(payload.EnumerateArray(), document => document.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task Delete_RemovesDocument()
    {
        var (id, client) = await UploadAndAcceptAsync("Doomed document");
        await WaitForStatusAsync(client, id, "ready");

        var deleteResponse = await client.DeleteAsync($"/api/documents/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/documents/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task UploadText_WithMissingTitle_Returns400()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/documents/text",
            new { title = "", content = "some content", sourceType = "text" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
