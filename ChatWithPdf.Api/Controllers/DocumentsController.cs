using ChatWithPdf.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithPdf.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController(
    IDocumentIngestionService ingestionService,
    IDocumentQueryService documentQueryService) : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentIngestionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentIngestionResult>> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { error = "Upload a non-empty PDF file." });
        }

        if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only PDF files are supported." });
        }

        await using var stream = file.OpenReadStream();
        var result = await ingestionService.IngestAsync(
            file.FileName,
            file.ContentType,
            stream,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentSummary>>> List(CancellationToken cancellationToken)
    {
        var documents = await documentQueryService.ListAsync(cancellationToken);
        return Ok(documents);
    }
}
