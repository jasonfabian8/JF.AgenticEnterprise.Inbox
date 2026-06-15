namespace JF.AgenticEnterprise.Domain.Entities;

public class TaxonomyCandidate
{
    public string Id { get; set; } = default!;
    public string EmailId { get; set; } = default!;
    public string? ProposalId { get; set; }
    public string ExtractedSignalsJson { get; set; } = "[]";
    public float MatchConfidence { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Email Email { get; set; } = default!;
    public TaxonomyProposal? Proposal { get; set; }
}
