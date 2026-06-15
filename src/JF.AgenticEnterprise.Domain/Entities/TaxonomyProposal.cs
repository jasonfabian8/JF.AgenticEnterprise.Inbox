namespace JF.AgenticEnterprise.Domain.Entities;

public class TaxonomyProposal
{
    public string Id { get; set; } = default!;
    public string SuggestedLabel { get; set; } = default!;

    /// <summary>
    /// Workflow that triggered this proposal via Taxonomy-Evolution-Agent.
    /// Null for proposals created outside a workflow context.
    /// </summary>
    public string? WorkflowId { get; set; }
    public string? EmailId { get; set; }
    public string Status { get; set; } = "PENDING";
    public float Confidence { get; set; }
    public int SampleCount { get; set; }
    public string SampleEmailIdsJson { get; set; } = "[]";
    public string SignalsJson { get; set; } = "[]";
    public string SuggestedRouting { get; set; } = "operations";
    public string SuggestedExtractionFieldsJson { get; set; } = "[]";
    public string CreatedByAgent { get; set; } = "TaxonomyEvolutionAgent";
    public string? DecidedBy { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecisionNote { get; set; }
    public string? ResultingCategoryId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public TaxonomyCategory? ResultingCategory { get; set; }
    public ICollection<TaxonomyCandidate> Candidates { get; set; } = [];
}
