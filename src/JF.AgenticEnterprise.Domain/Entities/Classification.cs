namespace JF.AgenticEnterprise.Domain.Entities;

public class Classification
{
    public string Id { get; set; } = default!;
    public string EmailId { get; set; } = default!;
    public string AgentExecutionId { get; set; } = default!;
    public string CategoryType { get; set; } = default!;
    public float Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public string AlternativeTypesJson { get; set; } = "[]";
    public string SignalsDetectedJson { get; set; } = "[]";
    public string Source { get; set; } = "AGENT";
    public bool IsOverridden { get; set; }
    public string? OverriddenBy { get; set; }
    public DateTimeOffset? OverriddenAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Email Email { get; set; } = default!;
    public AgentExecution AgentExecution { get; set; } = default!;
}
