// InvoiceAnalysisDto and ContractAnalysisDto are defined in WorkflowDtos.cs (same namespace)
namespace JF.AgenticEnterprise.Application.DTOs;

// ── Ingest ──────────────────────────────────────────────────────────────────

public record IngestEmailRequest(
    string SenderEmail,
    string SenderName,
    string Subject,
    string BodyPlainText,
    string? BodyHtml = null,
    DateTimeOffset? ReceivedAt = null,
    List<AttachmentIngestDto>? Attachments = null);

public record AttachmentIngestDto(
    string Filename,
    string MimeType,
    long SizeBytes = 0);

public record IngestEmailResponse(
    string EmailId,
    string Status,
    DateTimeOffset IngestedAt);

// ── List ─────────────────────────────────────────────────────────────────────

public record EmailListResponse(
    List<EmailListItemDto> Items,
    int Total,
    int Page,
    int PageSize);

public record EmailListItemDto(
    string Id,
    string SenderEmail,
    string SenderName,
    string Subject,
    string Status,
    string? CategoryType,
    float? Confidence,
    int AttachmentCount,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? ProcessedAt,
    int ProcessingDurationMs,
    bool HasConflict,
    bool HumanReviewed);

// ── Detail ───────────────────────────────────────────────────────────────────

public record EmailDetailDto(
    string Id,
    string SenderEmail,
    string SenderName,
    string Subject,
    string BodyPlainText,
    string? BodyHtml,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset IngestedAt,
    DateTimeOffset? ProcessedAt,
    int ProcessingDurationMs,
    bool HasConflict,
    bool HumanReviewed,
    ClassificationDto? Classification,
    List<AttachmentDto> Attachments,
    // Sprint 1 (attachment-based extraction — kept for backward compat)
    InvoiceExtractionDto? InvoiceExtraction,
    ContractExtractionDto? ContractExtraction,
    // Sprint 2 (agent-based analysis)
    InvoiceAnalysisDto? InvoiceAnalysis,
    ContractAnalysisDto? ContractAnalysis);

public record ClassificationDto(
    string CategoryType,
    float Confidence,
    string Reasoning,
    string Source,
    bool IsOverridden);

public record AttachmentDto(
    string Id,
    string Filename,
    string MimeType,
    long SizeBytes,
    string? DocumentType,
    float DocumentTypeConfidence);

public record InvoiceExtractionDto(
    string? VendorName,
    float VendorNameConfidence,
    string? InvoiceNumber,
    string? InvoiceDate,
    string? DueDate,
    decimal? TotalAmount,
    decimal? TaxAmount,
    string? Currency,
    string? PoReference,
    string? PaymentTerms,
    string ValidationStatus,
    float OverallConfidence);

public record ContractExtractionDto(
    string? PartyA,
    string? PartyB,
    string? AgreementType,
    string? EffectiveDate,
    string? ExpiryDate,
    bool? AutoRenewal,
    int? AutoRenewalNoticeDays,
    decimal? LiabilityCapAmount,
    string? GoverningLaw,
    float OverallConfidence,
    List<RiskFlagDto> RiskFlags);

public record RiskFlagDto(
    string FlagType,
    string Severity,
    string Excerpt,
    float Confidence);
