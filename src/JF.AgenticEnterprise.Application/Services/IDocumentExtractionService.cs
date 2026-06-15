namespace JF.AgenticEnterprise.Application.Services;

/// <summary>
/// Extracts readable text from email attachments so specialized agents
/// can analyse document content without raw file access.
/// MVP: reads already-stored ExtractedText or returns a structured placeholder.
/// </summary>
public interface IDocumentExtractionService
{
    Task<DocumentExtractionResponse> ExtractAsync(
        DocumentExtractionRequest request,
        CancellationToken         ct = default);
}

public sealed record DocumentExtractionRequest(
    string EmailId,
    IReadOnlyList<AttachmentExtractionItem> Attachments);

public sealed record AttachmentExtractionItem(
    string  AttachmentId,
    string  Filename,
    string  MimeType,
    string  StoragePath,
    string? AlreadyExtractedText);

public sealed record DocumentExtractionResponse(
    IReadOnlyList<ExtractedAttachment> Results);

public sealed record ExtractedAttachment(
    string  AttachmentId,
    string  Filename,
    string  MimeType,
    string? ExtractedText,
    string  ExtractionStatus);

public static class ExtractionStatus
{
    public const string Extracted    = "EXTRACTED";
    public const string AlreadyHad   = "ALREADY_HAD";
    public const string Unsupported  = "UNSUPPORTED";
    public const string NoAttachment = "NO_ATTACHMENT";
    public const string Failed       = "FAILED";
}
