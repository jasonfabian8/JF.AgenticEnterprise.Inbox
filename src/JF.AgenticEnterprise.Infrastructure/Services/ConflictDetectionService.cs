using JF.AgenticEnterprise.Application.Agents;
using JF.AgenticEnterprise.Application.Services;
using JF.AgenticEnterprise.Domain.Common;
using JF.AgenticEnterprise.Domain.Entities;
using JF.AgenticEnterprise.Domain.Settings;

namespace JF.AgenticEnterprise.Infrastructure.Services;

/// <summary>
/// Stateless service — no I/O. Compares agent outputs and decides
/// whether a conflict exists. Returns null when everything is consistent.
/// </summary>
public sealed class ConflictDetectionService : IConflictDetectionService
{
    public AgentConflict? DetectCategoryMismatch(
        string workflowId,
        string emailId,
        ClassificationResult classification,
        string specializedAgentType,
        string specializedCategory,
        float specializedConfidence)
    {
        // Normalise for comparison: "Invoice" vs "invoice" should not conflict.
        var classCategory = classification.Category.Trim();
        var specCategory  = specializedCategory.Trim();

        // Categories match — no conflict.
        if (string.Equals(classCategory, specCategory, StringComparison.OrdinalIgnoreCase))
            return null;

        // Only flag if the specialized agent is actually more confident than classification,
        // meaning its conclusion carries weight and genuinely contradicts.
        if (specializedConfidence <= classification.Confidence * 0.90f)
            return null;

        return new AgentConflict
        {
            Id           = UlidGenerator.NewUlid(),
            WorkflowId   = workflowId,
            EmailId      = emailId,
            SourceAgent  = AgentTypes.Classification,
            TargetAgent  = specializedAgentType,
            ConflictType = ConflictKind.CategoryMismatch,
            Description  = $"Classification concluded \"{classCategory}\" " +
                           $"({classification.Confidence:P0} confidence) but " +
                           $"{specializedAgentType} concluded \"{specCategory}\" " +
                           $"({specializedConfidence:P0} confidence).",
            SourceValue      = classCategory,
            TargetValue      = specCategory,
            SourceConfidence = classification.Confidence,
            TargetConfidence = specializedConfidence,
            CreatedAt        = DateTimeOffset.UtcNow,
        };
    }

    public AgentConflict? DetectLowConfidence(
        string workflowId,
        string emailId,
        string agentType,
        string category,
        float confidence,
        WorkflowSettings settings)
    {
        if (confidence >= settings.MediumConfidenceThreshold)
            return null;

        return new AgentConflict
        {
            Id           = UlidGenerator.NewUlid(),
            WorkflowId   = workflowId,
            EmailId      = emailId,
            SourceAgent  = agentType,
            TargetAgent  = "System",
            ConflictType = ConflictKind.LowConfidence,
            Description  = $"{agentType} returned \"{category}\" with only " +
                           $"{confidence:P0} confidence (threshold: " +
                           $"{settings.MediumConfidenceThreshold:P0}). " +
                           "Cannot auto-accept — escalation required.",
            SourceValue      = category,
            TargetValue      = null,
            SourceConfidence = confidence,
            TargetConfidence = 0f,
            CreatedAt        = DateTimeOffset.UtcNow,
        };
    }
}
