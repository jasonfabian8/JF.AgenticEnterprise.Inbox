namespace JF.AgenticEnterprise.Application.Agents;

/// <summary>
/// Invokes Human-Collaboration-Agent in Azure AI Foundry.
/// Determines whether human intervention is required and formulates the review task:
/// what question to ask, what recommendation to make, and what priority to assign.
/// </summary>
public interface IHumanCollaborationAgent
{
    Task<HumanCollaborationResult> EvaluateAsync(
        HumanCollaborationRequest request,
        CancellationToken ct = default);
}

public sealed record HumanCollaborationRequest(
    string WorkflowId,
    string EmailId,
    string Subject,
    string BodyPlainText,

    string CurrentCategory,
    float CurrentConfidence,

    /// <summary>Why escalation was triggered (conflict description or low-confidence message).</summary>
    string EscalationReason,

    /// <summary>Category suggested by Taxonomy Evolution Agent, if it ran before this agent.</summary>
    string? TaxonomySuggestion,
    float? TaxonomySuggestionConfidence);

public sealed record HumanCollaborationResult(
    /// <summary>
    /// When false, the agent determined the system can proceed automatically
    /// (e.g. taxonomy suggestion was high-confidence enough).
    /// </summary>
    bool RequiresHumanReview,

    /// <summary>One of <see cref="Domain.Entities.ReviewType"/> constants.</summary>
    string ReviewType,

    /// <summary>The specific question to surface in the human review UI.</summary>
    string Question,

    /// <summary>Agent's recommended action for the reviewer.</summary>
    string Recommendation,

    /// <summary>One of <see cref="Domain.Entities.ReviewPriority"/> constants.</summary>
    string Priority,

    string Reasoning,
    string RawOutputJson);
