using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecAssist.Application.Common;
using TecAssist.Application.Contracts;
using TecAssist.Application.Conversations;
using TecAssist.Application.Rag;

namespace TecAssist.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/conversations")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class ConversationsController(
    ConversationService conversationService,
    ChatService chatService,
    ILogger<ConversationsController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        var conversation = await conversationService.CreateAsync(request.Title, cancellationToken);
        return CreatedAtAction(nameof(GetMessages), new { id = conversation.Id }, conversation);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ConversationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        return Ok(await conversationService.ListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}/messages")]
    [ProducesResponseType(typeof(IReadOnlyList<MessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(Guid id, CancellationToken cancellationToken)
    {
        var messages = await conversationService.GetMessagesAsync(id, cancellationToken);
        return messages is null ? NotFound() : Ok(messages);
    }

    [HttpPost("{id:guid}/title")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateTitle(Guid id, CancellationToken cancellationToken)
    {
        var title = await chatService.GenerateTitleAsync(id, cancellationToken);
        return Ok(new { id, title });
    }

    [HttpPost("{id:guid}/messages")]
    [Produces(MediaTypeNames.Text.EventStream)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task SendMessage(
        Guid id,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var stream = await chatService.StartMessageStreamAsync(id, request.Content, cancellationToken);

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";

        try
        {
            await foreach (var streamEvent in stream.Events.WithCancellation(cancellationToken))
            {
                switch (streamEvent)
                {
                    case TokenEvent tokenEvent:
                        await WriteFrameAsync("token", new { text = tokenEvent.Text }, cancellationToken);
                        break;
                    case CitationsEvent citationsEvent:
                        foreach (var citation in citationsEvent.Citations)
                        {
                            await WriteFrameAsync("citation", citation, cancellationToken);
                        }
                        break;
                    case CompletedEvent completedEvent:
                        await WriteFrameAsync("done", new
                        {
                            userMessageId = completedEvent.UserMessageId,
                            assistantMessageId = completedEvent.AssistantMessageId
                        }, cancellationToken);
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ChatGenerationException exception)
        {
            logger.LogError(exception, "Chat generation failed mid-stream for conversation {ConversationId}", id);
            await WriteFrameAsync("error", new
            {
                message = "The AI service failed while generating the answer. Please retry."
            }, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected failure while streaming conversation {ConversationId}", id);
            await WriteFrameAsync("error", new
            {
                message = "An unexpected error occurred while generating the answer."
            }, CancellationToken.None);
        }
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rename(
        Guid id,
        [FromBody] UpdateConversationRequest request,
        CancellationToken cancellationToken)
    {
        var conversation = await conversationService.RenameAsync(id, request.Title, cancellationToken);
        return conversation is null ? NotFound() : Ok(conversation);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await conversationService.DeleteAsync(id, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private async Task WriteFrameAsync(string eventName, object payload, CancellationToken cancellationToken)
    {
        await Response.WriteAsync(
            $"event: {eventName}\ndata: {JsonSerializer.Serialize(payload, JsonOptions.Options)}\n\n",
            cancellationToken);
    }

    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    }
}
