using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using TecAssist.Application.Contracts;
using TecAssist.Application.Documents;

namespace TecAssist.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class DocumentsController(DocumentService documentService) : ControllerBase
{
    private const int MaxUploadBytes = 25 * 1024 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaxUploadBytes)]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "The uploaded file is empty." });
        }

        if (file.Length > MaxUploadBytes)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "The uploaded file exceeds the 25 MB limit." });
        }

        await using var stream = file.OpenReadStream();
        var response = await documentService.CreateFromFileAsync(file.FileName, stream, cancellationToken);
        return AcceptedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPost("text")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadText(
        [FromBody] CreateTextDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await documentService.CreateFromTextAsync(request, cancellationToken);
        return AcceptedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        return Ok(await documentService.ListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await documentService.GetAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await documentService.DeleteAsync(id, cancellationToken)
            ? NoContent()
            : NotFound();
    }
}
