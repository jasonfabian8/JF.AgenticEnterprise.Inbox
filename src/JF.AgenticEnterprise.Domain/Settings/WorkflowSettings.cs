namespace JF.AgenticEnterprise.Domain.Settings;

/// <summary>
/// Confidence-based routing thresholds. Loaded from appsettings "WorkflowSettings" section.
/// All thresholds are 0.0–1.0 (e.g. 0.85 = 85%).
/// </summary>
public sealed class WorkflowSettings
{
    public const string Section = "WorkflowSettings";

    /// <summary>
    /// At or above this threshold the workflow auto-accepts the classification
    /// without invoking escalation agents.
    /// Default: 0.85 (85%).
    /// </summary>
    public float HighConfidenceThreshold { get; set; } = 0.85f;

    /// <summary>
    /// At or above this threshold (but below High) the workflow continues normally.
    /// Below this threshold escalation is triggered.
    /// Default: 0.70 (70%).
    /// </summary>
    public float MediumConfidenceThreshold { get; set; } = 0.70f;

    /// <summary>
    /// When enabled, low-confidence and conflict cases invoke Taxonomy-Evolution-Agent.
    /// </summary>
    public bool EnableTaxonomyEvolution { get; set; } = true;

    /// <summary>
    /// When enabled, low-confidence and conflict cases invoke Human-Collaboration-Agent.
    /// </summary>
    public bool EnableHumanCollaboration { get; set; } = true;

    // ── Derived helpers ────────────────────────────────────────────────────────

    public ConfidenceBand GetBand(float confidence) => confidence switch
    {
        _ when confidence >= HighConfidenceThreshold   => ConfidenceBand.High,
        _ when confidence >= MediumConfidenceThreshold => ConfidenceBand.Medium,
        _                                              => ConfidenceBand.Low,
    };
}

/// <summary>Routing band derived from a confidence score against configured thresholds.</summary>
public enum ConfidenceBand
{
    /// <summary>≥85% — auto-accept, no escalation.</summary>
    High,

    /// <summary>70–84% — continue workflow normally.</summary>
    Medium,

    /// <summary>&lt;70% — escalate to Taxonomy Evolution + Human Collaboration.</summary>
    Low,
}
