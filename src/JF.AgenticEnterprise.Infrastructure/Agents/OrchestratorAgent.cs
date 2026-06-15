using JF.AgenticEnterprise.Application.Agents;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JF.AgenticEnterprise.Infrastructure.Agents;

public sealed class OrchestratorAgent : IOrchestratorAgent
{
    private readonly IAgentRuntime _runtime;
    private readonly string _agentId;
    private readonly ILogger<OrchestratorAgent> _logger;

    private const string SystemPrompt = """
        You are an intelligent workflow orchestrator for a business document processing system.

        Given a classification result, determine the most appropriate next processing step.

        Routing rules (apply in order):
        - "Invoice"             → nextAgent: "InvoiceAgent"
        - "Contract"            → nextAgent: "ContractAgent"
        - "Commercial Proposal" → nextAgent: "ContractAgent"
        - "Information Request" → nextAgent: "Complete"
        - "Marketing"           → nextAgent: "Complete"
        - "Bank Statement"      → nextAgent: "Complete"
        - "Unknown"             → nextAgent: "HumanReview"

        workflowStatus mapping:
        - InvoiceAgent   → "PROCESSING"
        - ContractAgent  → "PROCESSING"
        - Complete       → "COMPLETED_AUTO"
        - HumanReview    → "AWAITING_REVIEW"

        Respond ONLY with valid JSON — no markdown, no explanation outside the JSON:
        {
          "nextAgent": "InvoiceAgent|ContractAgent|HumanReview|Complete",
          "workflowStatus": "PROCESSING|AWAITING_REVIEW|COMPLETED_AUTO",
          "reasoning": "<1-2 sentences explaining the routing decision>"
        }
        """;

    public OrchestratorAgent(
        IAgentRuntime runtime,
        AiProviderOptions options,
        ILogger<OrchestratorAgent> logger)
    {
        _runtime = runtime;
        _agentId = options.OrchestratorAgentId;
        _logger = logger;
    }

    public async Task<OrchestratorResult> DecideAsync(
        OrchestratorRequest request,
        CancellationToken ct = default)
    {
        var userMessage = $"""
            Workflow ID: {request.WorkflowId}
            Email ID: {request.EmailId}

            Classification result:
            - Category: {request.ClassificationCategory}
            - Confidence: {request.ClassificationConfidence:P0}
            - Reasoning: {request.ClassificationReasoning}

            Determine the appropriate next agent and workflow status.
            """;

        _logger.LogDebug(
            "OrchestratorAgent invoking {AgentId} for category {Category}",
            _agentId, request.ClassificationCategory);

        var response = await _runtime.InvokeAsync(
            new AgentRuntimeRequest(_agentId, SystemPrompt, userMessage), ct);

        var result = ParseResponse(response.Content);

        _logger.LogInformation(
            "OrchestratorAgent decision: nextAgent={NextAgent}, status={Status}",
            result.NextAgent, result.WorkflowStatus);

        return result;
    }

    private static OrchestratorResult ParseResponse(string content)
    {
        var json = StripMarkdownFences(content.Trim());

        JsonElement root;
        try
        {
            root = JsonDocument.Parse(json).RootElement;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Orchestrator agent returned malformed JSON: {json}", ex);
        }

        if (!root.TryGetProperty("nextAgent", out var nextAgentProp) ||
            !root.TryGetProperty("workflowStatus", out var statusProp) ||
            !root.TryGetProperty("reasoning", out var reasoningProp))
        {
            throw new InvalidOperationException(
                $"Orchestrator response missing required fields. Raw: {json}");
        }

        var nextAgent = nextAgentProp.GetString() ?? NextAgentName.HumanReview;

        // Guard against hallucinated next-agent values
        if (!ValidNextAgents.Contains(nextAgent))
            nextAgent = NextAgentName.HumanReview;

        return new OrchestratorResult(
            NextAgent: nextAgent,
            WorkflowStatus: statusProp.GetString() ?? "PROCESSING",
            Reasoning: reasoningProp.GetString() ?? string.Empty);
    }

    private static readonly HashSet<string> ValidNextAgents =
    [
        NextAgentName.InvoiceAgent,
        NextAgentName.ContractAgent,
        NextAgentName.HumanReview,
        NextAgentName.Complete,
    ];

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
