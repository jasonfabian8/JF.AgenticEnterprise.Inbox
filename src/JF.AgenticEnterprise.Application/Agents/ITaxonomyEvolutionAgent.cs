namespace JF.AgenticEnterprise.Application.Agents;

/// <summary>
/// Invokes Taxonomy-Evolution-Agent in Azure AI Foundry.
/// Called when classification confidence is low OR when a category mismatch conflict is detected.
/// The agent analyses whether the document fits any existing category or needs a new one.
/// </summary>
public interface ITaxonomyEvolutionAgent
{
    Task<TaxonomyEvolutionResult> AnalyzeAsync(
        TaxonomyEvolutionRequest request,
        CancellationToken ct = default);
}

public sealed record TaxonomyEvolutionRequest(
    string WorkflowId,
    string EmailId,
    string Subject,
    string BodyPlainText,

    /// <summary>Best category the system has so far (may be disputed).</summary>
    string CurrentCategory,
    float CurrentConfidence,

    /// <summary>Human-readable description of why escalation was triggered.</summary>
    string EscalationReason,

    /// <summary>Full list of categories currently in the taxonomy.</summary>
    IReadOnlyList<string> ExistingCategories);

public sealed record TaxonomyEvolutionResult(
    /// <summary>True when the agent believes a new category should be created.</summary>
    bool NewCategorySuggested,

    /// <summary>Proposed name for the new category (null when NewCategorySuggested is false).</summary>
    string? SuggestedCategory,

    /// <summary>Best-fit category from existing taxonomy (may differ from CurrentCategory).</summary>
    string? BestFitExistingCategory,

    float Confidence,
    string Reasoning,
    string RawOutputJson);
