namespace JF.AgenticEnterprise.Domain.Entities;

/// <summary>
/// Structured contract data extracted by the Contract Agent during Sprint 2 processing.
/// Distinct from <see cref="ContractExtraction"/> (Sprint 1 attachment-based extraction).
/// Linked directly to Email for fast retrieval via a single Include.
/// </summary>
public class ContractAnalysis
{
    public string Id { get; set; } = default!;

    public string EmailId { get; set; } = default!;

    public string WorkflowId { get; set; } = default!;

    public string AgentExecutionId { get; set; } = default!;

    // ── Extracted fields ──────────────────────────────────────────────────────

    public string? ContractType { get; set; }

    /// <summary>JSON array of party names: ["Acme Corp", "Beta Ltd"]</summary>
    public string PartiesJson { get; set; } = "[]";

    public string? EffectiveDate { get; set; }

    public string? ExpirationDate { get; set; }

    public string? RenewalClause { get; set; }

    /// <summary>JSON array of obligation strings.</summary>
    public string KeyObligationsJson { get; set; } = "[]";

    // ── Quality indicators ────────────────────────────────────────────────────

    public float Confidence { get; set; }

    public string Reasoning { get; set; } = string.Empty;

    /// <summary>Raw JSON returned by the agent for debugging/audit.</summary>
    public string RawOutputJson { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public Email Email { get; set; } = default!;
    public Workflow Workflow { get; set; } = default!;
    public AgentExecution AgentExecution { get; set; } = default!;
}
