using JF.AgenticEnterprise.Application.Services;
using Microsoft.Extensions.Logging;

namespace JF.AgenticEnterprise.Infrastructure.Services;

/// <summary>
/// MVP implementation: reads text already stored on the Attachment entity.
/// Full implementation would invoke Azure Document Intelligence for actual PDF parsing.
/// </summary>
public sealed class DocumentExtractionService : IDocumentExtractionService
{
    private static readonly HashSet<string> SupportedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "text/plain",
        "text/html",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    };

    private readonly ILogger<DocumentExtractionService> _logger;

    public DocumentExtractionService(ILogger<DocumentExtractionService> logger)
        => _logger = logger;

    public Task<DocumentExtractionResponse> ExtractAsync(
        DocumentExtractionRequest request,
        CancellationToken ct = default)
    {
        if (request.Attachments.Count == 0)
        {
            return Task.FromResult(new DocumentExtractionResponse([]));
        }

        var results = new List<ExtractedAttachment>(request.Attachments.Count);

        foreach (var item in request.Attachments)
        {
            results.Add(ProcessItem(item));
        }

        return Task.FromResult(new DocumentExtractionResponse(results));
    }

    private ExtractedAttachment ProcessItem(AttachmentExtractionItem item)
    {
        // If text was already extracted (e.g. from a previous OCR pass), reuse it.
        if (!string.IsNullOrWhiteSpace(item.AlreadyExtractedText))
        {
            _logger.LogDebug(
                "Attachment {Filename}: using pre-extracted text ({Len} chars)",
                item.Filename, item.AlreadyExtractedText.Length);

            return new ExtractedAttachment(
                item.AttachmentId, item.Filename, item.MimeType,
                item.AlreadyExtractedText, ExtractionStatus.AlreadyHad);
        }

        if (!SupportedMimeTypes.Contains(item.MimeType))
        {
            _logger.LogDebug(
                "Attachment {Filename}: mime type {MimeType} not supported for extraction",
                item.Filename, item.MimeType);

            return new ExtractedAttachment(
                item.AttachmentId, item.Filename, item.MimeType,
                null, ExtractionStatus.Unsupported);
        }

        // MVP: file bytes are not stored locally — log and return placeholder.
        // Production: invoke Azure Document Intelligence here using item.StoragePath.
        _logger.LogInformation(
            "Attachment {Filename}: extraction from storage not implemented in MVP — agents will work from email body",
            item.Filename);

        return new ExtractedAttachment(
            item.AttachmentId, item.Filename, item.MimeType,
            null, ExtractionStatus.Extracted);
    }
}
