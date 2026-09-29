using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using TecAssist.Application.Contracts;
using TecAssist.Application.Conversations;
using TecAssist.Application.Rag;

namespace TecAssist.Api.Controllers;

[ApiController]
[Route("api/conversations")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class ConversationsController(ConversationService conversationService, ChatService chatService) : ControllerBase
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

    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(
        Guid id,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var response = await chatService.SendMessageAsync(id, request.Content, cancellationToken);
        return response is null ? NotFound() : Ok(response);
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
}
