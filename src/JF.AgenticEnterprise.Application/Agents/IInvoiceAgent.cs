namespace JF.AgenticEnterprise.Application.Agents;

public interface IInvoiceAgent
{
    Task<InvoiceAnalysisResult> ExtractAsync(
        InvoiceExtractionRequest request,
        CancellationToken ct = default);
}

public sealed record InvoiceExtractionRequest(
    string WorkflowId,
    string EmailId,
    string Subject,
    string BodyPlainText,
    IReadOnlyList<AttachmentContext> Attachments);

public sealed record InvoiceAnalysisResult(
    string? Supplier,
    string? InvoiceNumber,
    string? InvoiceDate,
    string? DueDate,
    string? Currency,
    decimal? TotalAmount,
    float Confidence,
    string Summary,
    string RawOutputJson);

public sealed record AttachmentContext(
    string Filename,
    string MimeType,
    string? ExtractedText);
