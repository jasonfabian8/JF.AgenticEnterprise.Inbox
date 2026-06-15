namespace JF.AgenticEnterprise.Domain.Entities;

public class WorkflowStep
{
    public string Id { get; set; } = default!;
    public string WorkflowId { get; set; } = default!;
    public int StepOrder { get; set; }
    public string StepName { get; set; } = default!;
    public string? AgentType { get; set; }
    public string Status { get; set; } = WorkflowStepStatus.Pending;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int DurationMs { get; set; }
    public string? InputSummary { get; set; }
    public string? OutputSummary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Workflow Workflow { get; set; } = default!;
}

public static class WorkflowStepStatus
{
    public const string Pending   = "PENDING";
    public const string Running   = "RUNNING";
    public const string Completed = "COMPLETED";
    public const string Skipped   = "SKIPPED";
    public const string Failed    = "FAILED";
}

public static class WorkflowStepName
{
    public const string Classifying          = "CLASSIFYING";
    public const string AnalyzingDocuments   = "ANALYZING_DOCUMENTS";
    public const string CrossValidating      = "CROSS_VALIDATING";
    public const string ExtractingInvoice    = "EXTRACTING_INVOICE";
    public const string ExtractingContract   = "EXTRACTING_CONTRACT";
    public const string AnalyzingTaxonomy    = "ANALYZING_TAXONOMY";
    public const string HumanReview          = "HUMAN_REVIEW";
    public const string Completing           = "COMPLETING";
}
