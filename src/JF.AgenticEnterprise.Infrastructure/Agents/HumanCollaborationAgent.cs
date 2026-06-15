using JF.AgenticEnterprise.Application.Agents;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JF.AgenticEnterprise.Infrastructure.Agents;

public sealed class HumanCollaborationAgent : IHumanCollaborationAgent
{
    private readonly IAgentRuntime _runtime;
    private readonly string _agentId;
    private readonly string _agentVersion;
    private readonly ILogger<HumanCollaborationAgent> _logger;

    private const string SystemPrompt = """
        You are an expert escalation coordinator for a business document management system.

        The system could not confidently classify or process a document automatically.
        Your job is to:
        1. Decide whether a human reviewer is actually needed, or if the system can proceed
           with available information.
        2. If a human is needed, formulate a clear, actionable review task:
           - Write a specific question the reviewer must answer.
           - Provide a concrete recommendation based on available evidence.
           - Assign priority: URGENT (blocking business process), NORMAL, or LOW.

        Respond ONLY with valid JSON — no markdown, no explanation outside the JSON:
        {
          "requiresHumanReview": true,
          "reviewType":          "CLASSIFICATION_OVERRIDE",
          "question":            "",
          "recommendation":      "",
          "priority":            "NORMAL",
          "reasoning":           ""
        }

        Valid reviewType values: EXTRACTION_CORRECTION, CLASSIFICATION_OVERRIDE,
        TAXONOMY_PROPOSAL, CONFLICT_RESOLUTION, RISK_FLAGS, AGENT_FAILURE.

        Rules:
        - Set requiresHumanReview to false ONLY when the taxonomy suggestion has
          confidence ≥ 0.80 and no conflict remains.
        - "question" must be answerable in one decision (e.g. "Is this an Invoice or a Contract?").
        - "recommendation" must be actionable (e.g. "Based on the invoice number in the subject,
          classify as Invoice.").
        - Keep reasoning to 1-2 sentences.
        """;

    public HumanCollaborationAgent(
        IAgentRuntime runtime,
        AiProviderOptions options,
        ILogger<HumanCollaborationAgent> logger)
    {
        _runtime      = runtime;
        _agentId      = options.HumanCollaborationAgentId;
        _agentVersion = options.HumanCollaborationAgentVersion;
        _logger       = logger;
    }

    public async Task<HumanCollaborationResult> EvaluateAsync(
        HumanCollaborationRequest request,
        CancellationToken ct = default)
    {
        var userMessage = BuildUserMessage(request);

        _logger.LogDebug(
            "HumanCollaborationAgent invoking {AgentId} for workflow {WorkflowId}",
            _agentId, request.WorkflowId);

        var response = await _runtime.InvokeAsync(
            new AgentRuntimeRequest(_agentId, _agentVersion, SystemPrompt, userMessage), ct);

        var result = ParseResponse(response.Content);

        _logger.LogInformation(
            "HumanCollaborationAgent: requiresReview={Required}, type={Type}, priority={Priority}",
            result.RequiresHumanReview, result.ReviewType, result.Priority);

        return result;
    }

    private static string BuildUserMessage(HumanCollaborationRequest request)
    {
        var body = request.BodyPlainText.Length > 2000
            ? request.BodyPlainText[..2000] + "\n[... truncated]"
            : request.BodyPlainText;

        var taxonomySection = request.TaxonomySuggestion is not null
            ? $"Taxonomy Evolution Agent suggested: \"{request.TaxonomySuggestion}\" " +
              $"(confidence: {request.TaxonomySuggestionConfidence:P0})"
            : "Taxonomy Evolution Agent: not invoked";

        return $"""
            Escalation reason: {request.EscalationReason}
            Current classification: {request.CurrentCategory} (confidence: {request.CurrentConfidence:P0})
            {taxonomySection}

            Subject: {request.Subject}

            Body:
            {body}

            Determine whether a human reviewer is required and formulate the review task.
            """;
    }

    private static HumanCollaborationResult ParseResponse(string content)
    {
        var json = StripMarkdownFences(content.Trim());

        JsonElement root;
        try { root = JsonDocument.Parse(json).RootElement; }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"HumanCollaborationAgent returned malformed JSON: {json}", ex);
        }

        bool requiresReview = true;
        if (root.TryGetProperty("requiresHumanReview", out var reqProp))
            requiresReview = reqProp.GetBoolean();

        return new HumanCollaborationResult(
            RequiresHumanReview: requiresReview,
            ReviewType:          GetString(root, "reviewType")     ?? ReviewType.ClassificationOverride,
            Question:            GetString(root, "question")       ?? "Please review this document.",
            Recommendation:      GetString(root, "recommendation") ?? string.Empty,
            Priority:            GetString(root, "priority")       ?? ReviewPriority.Normal,
            Reasoning:           GetString(root, "reasoning")      ?? string.Empty,
            RawOutputJson:       json);
    }

    private static string? GetString(JsonElement root, string key) =>
        root.TryGetProperty(key, out var p) && p.ValueKind != JsonValueKind.Null
            ? p.GetString()
            : null;

    private static string StripMarkdownFences(string text)
    {
        if (!text.StartsWith("```", StringComparison.Ordinal)) return text;
        var newline = text.IndexOf('\n');
        if (newline < 0) return text;
        var end = text.LastIndexOf("```", StringComparison.Ordinal);
        if (end <= newline) return text;
        return text[(newline + 1)..end].Trim();
    }
}
