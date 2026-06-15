namespace JF.AgenticEnterprise.Domain.Entities;

public class AuditEntry
{
    public string Id { get; set; } = default!;
    public string? EmailId { get; set; }
    public string EntityType { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string ActorType { get; set; } = default!;
    public string ActorId { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string? BeforeValueJson { get; set; }
    public string? AfterValueJson { get; set; }
    public string? Reasoning { get; set; }
    public DateTimeOffset OccurredAt { get; set; }

    public Email? Email { get; set; }
}

public static class AuditActorType
{
    public const string Agent = "AGENT";
    public const string Human = "HUMAN";
    public const string System = "SYSTEM";
}

public static class AuditAction
{
    public const string EmailIngested = "EMAIL_INGESTED";
    public const string WorkflowStarted = "WORKFLOW_STARTED";
    public const string WorkflowCompleted = "WORKFLOW_COMPLETED";
    public const string ClassificationOverridden = "CLASSIFICATION_OVERRIDDEN";
    public const string ReviewDecided = "REVIEW_DECIDED";
    public const string FieldCorrected = "FIELD_CORRECTED";
    public const string TaxonomyCategoryCreated = "TAXONOMY_CATEGORY_CREATED";
    public const string TaxonomyProposalApproved = "TAXONOMY_PROPOSAL_APPROVED";
    public const string TaxonomyProposalDismissed = "TAXONOMY_PROPOSAL_DISMISSED";
    public const string EmailReclassified = "EMAIL_RECLASSIFIED";
}
