namespace JF.AgenticEnterprise.Domain.Entities;

public class Workflow
{
    public string Id { get; set; } = default!;
    public string EmailId { get; set; } = default!;
    public string Status { get; set; } = WorkflowStatus.Queued;
    public string? CurrentStep { get; set; }
    public string? ConflictReportJson { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
    public string? OutcomeType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Email Email { get; set; } = default!;
    public ICollection<WorkflowStep>       Steps              { get; set; } = [];
    public ICollection<AgentExecution>     AgentExecutions    { get; set; } = [];
    public ICollection<HumanReview>        HumanReviews       { get; set; } = [];

    // ── Sprint 2 nav props ────────────────────────────────────────────────────
    public OrchestrationDecision? OrchestrationDecision { get; set; }
    public WorkflowResult?        WorkflowResult        { get; set; }
    public InvoiceAnalysis?       InvoiceAnalysis       { get; set; }
    public ContractAnalysis?      ContractAnalysis      { get; set; }
}

public static class WorkflowStatus
{
    public const string Queued         = "QUEUED";
    public const string Processing     = "PROCESSING";
    public const string AwaitingReview = "AWAITING_REVIEW";
    public const string CompletedAuto  = "COMPLETED_AUTO";
    public const string CompletedHuman = "COMPLETED_HUMAN";
    public const string Failed         = "FAILED";
}
