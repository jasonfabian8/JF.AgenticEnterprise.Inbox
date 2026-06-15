namespace JF.AgenticEnterprise.Domain.Entities;

/// <summary>
/// The single source of truth for what the system currently believes about a document.
/// Updated by each reasoning agent as the workflow progresses, enabling the
/// "Workflow Knowledge View" in the frontend to show how understanding evolved.
/// </summary>
public class WorkflowKnowledge
{
    public string Id { get; set; } = default!;
    public string WorkflowId { get; set; } = default!;
    public string EmailId { get; set; } = default!;

    // ── Phase 1: Classification Agent ─────────────────────────────────────────
    public string InitialCategory { get; set; } = string.Empty;
    public float InitialConfidence { get; set; }

    // ── Phase 2: Specialized Agent (Invoice / Contract) ───────────────────────
    public string? RefinedCategory { get; set; }
    public float? RefinedConfidence { get; set; }
    public string? RefinedReasoning { get; set; }

    // ── Phase 3: Taxonomy Evolution Agent (if conflict / low confidence) ──────
    public string? SuggestedCategory { get; set; }
    public float? SuggestionConfidence { get; set; }
    public string? SuggestionReasoning { get; set; }

    // ── Phase 4: Human Decision ───────────────────────────────────────────────
    public string? ApprovedCategory { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }

    // ── Current working state (updated by each phase) ─────────────────────────
    public string CurrentCategory { get; set; } = string.Empty;
    public float CurrentConfidence { get; set; }
    public string CurrentReasoning { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // ── Navigation ─────────────────────────────────────────────────────────────
    public Workflow Workflow { get; set; } = default!;
    public Email Email { get; set; } = default!;
}
