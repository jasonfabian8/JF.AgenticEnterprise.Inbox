namespace JF.AgenticEnterprise.Domain.Entities;

/// <summary>
/// Structured invoice data extracted by the Invoice Agent during Sprint 2 processing.
/// Distinct from <see cref="InvoiceExtraction"/> (Sprint 1 attachment-based extraction).
/// Linked directly to Email for fast retrieval via a single Include.
/// </summary>
public class InvoiceAnalysis
{
    public string Id { get; set; } = default!;

    public string EmailId { get; set; } = default!;

    public string WorkflowId { get; set; } = default!;

    public string AgentExecutionId { get; set; } = default!;

    // ── Extracted fields ──────────────────────────────────────────────────────

    public string? Supplier { get; set; }

    public string? InvoiceNumber { get; set; }

    public string? InvoiceDate { get; set; }

    public string? DueDate { get; set; }

    public string? Currency { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? Summary { get; set; }

    // ── Quality indicators ────────────────────────────────────────────────────

    public float Confidence { get; set; }

    /// <summary>Raw JSON returned by the agent for debugging/audit.</summary>
    public string RawOutputJson { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public Email          Email          { get; set; } = default!;
    public Workflow       Workflow       { get; set; } = default!;
    public AgentExecution AgentExecution { get; set; } = default!;
}
