namespace JF.AgenticEnterprise.Domain.Entities;

public class Email
{
    public string Id { get; set; } = default!;
    public string IdempotencyKey { get; set; } = default!;
    public string Source { get; set; } = "MANUAL_UPLOAD";
    public string SenderEmail { get; set; } = default!;
    public string SenderName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyPlainText { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? RawStoragePath { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset IngestedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string Status { get; set; } = EmailStatus.Queued;
    public int ProcessingDurationMs { get; set; }
    public bool HasConflict { get; set; }
    public bool HumanReviewed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Attachment>        Attachments        { get; set; } = [];
    public Workflow?                      Workflow           { get; set; }
    public Classification?                Classification     { get; set; }
    public InvoiceExtraction?             InvoiceExtraction  { get; set; }
    public ContractExtraction?            ContractExtraction { get; set; }
    public ICollection<AgentExecution>    AgentExecutions    { get; set; } = [];
    public ICollection<HumanReview>       HumanReviews       { get; set; } = [];
    public ICollection<TaxonomyCandidate> TaxonomyCandidates { get; set; } = [];
    public ICollection<AuditEntry>        AuditEntries       { get; set; } = [];

    // ── Sprint 2 analysis results (populated by Invoice/Contract agents) ───────
    public InvoiceAnalysis?  InvoiceAnalysis  { get; set; }
    public ContractAnalysis? ContractAnalysis { get; set; }
}

public static class EmailStatus
{
    public const string Queued         = "QUEUED";
    public const string Processing     = "PROCESSING";
    public const string AwaitingReview = "AWAITING_REVIEW";
    public const string CompletedAuto  = "COMPLETED_AUTO";
    public const string CompletedHuman = "COMPLETED_HUMAN";
    public const string Failed         = "FAILED";
    public const string Rejected       = "REJECTED";
}
