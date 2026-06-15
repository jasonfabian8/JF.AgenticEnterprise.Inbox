using JF.AgenticEnterprise.Application.Agents;
using JF.AgenticEnterprise.Domain.Entities;
using JF.AgenticEnterprise.Domain.Settings;

namespace JF.AgenticEnterprise.Application.Services;

/// <summary>
/// Pure domain service — stateless, no I/O.
/// Compares agent outputs and decides whether a conflict exists.
/// Consumed by WorkflowOrchestrator after each agent runs.
/// </summary>
public interface IConflictDetectionService
{
    /// <summary>
    /// Compares the Classification Agent's category with a specialized agent's conclusion.
    /// Returns a new AgentConflict when the categories differ significantly; null otherwise.
    /// </summary>
    AgentConflict? DetectCategoryMismatch(
        string workflowId,
        string emailId,
        ClassificationResult classification,
        string specializedAgentType,
        string specializedCategory,
        float specializedConfidence);

    /// <summary>
    /// Returns a conflict when <paramref name="confidence"/> is below
    /// <see cref="WorkflowSettings.MediumConfidenceThreshold"/>.
    /// </summary>
    AgentConflict? DetectLowConfidence(
        string workflowId,
        string emailId,
        string agentType,
        string category,
        float confidence,
        WorkflowSettings settings);
}
