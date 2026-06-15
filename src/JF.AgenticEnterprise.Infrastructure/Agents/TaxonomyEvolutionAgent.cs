using JF.AgenticEnterprise.Application.Agents;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JF.AgenticEnterprise.Infrastructure.Agents;

public sealed class TaxonomyEvolutionAgent : ITaxonomyEvolutionAgent
{
    private readonly IAgentRuntime _runtime;
    private readonly string _agentId;
    private readonly string _agentVersion;
    private readonly ILogger<TaxonomyEvolutionAgent> _logger;

    private const string SystemPrompt = """
        You are an expert document taxonomy analyst for a business document management system.

        You will be given an email that the system had difficulty classifying with high confidence,
        along with the full list of currently known document categories.

        Your job:
        1. Determine whether the document fits an existing category (perhaps one other than the
           system's initial guess).
        2. If it does not fit any existing category well, propose a new category name.

        Respond ONLY with valid JSON — no markdown, no explanation outside the JSON:
        {
          "newCategorySuggested":      false,
          "suggestedCategory":         null,
          "bestFitExistingCategory":   "Invoice",
          "confidence":                0.0,
          "reasoning":                 ""
        }

        Rules:
        - "newCategorySuggested" is true ONLY when no existing category is a reasonable fit.
        - "suggestedCategory" is a concise, title-case name for the proposed new category
          (null when newCategorySuggested is false).
        - "bestFitExistingCategory" is always filled — pick the closest existing category,
          even if confidence is low.
        - "confidence" is your confidence in the chosen/suggested category, 0.0 to 1.0.
        - "reasoning" explains the key signals that led to your conclusion (1-3 sentences).
        """;

    public TaxonomyEvolutionAgent(
        IAgentRuntime runtime,
        AiProviderOptions options,
        ILogger<TaxonomyEvolutionAgent> logger)
    {
        _runtime      = runtime;
        _agentId      = options.TaxonomyEvolutionAgentId;
        _agentVersion = options.TaxonomyEvolutionAgentVersion;
        _logger       = logger;
    }

    public async Task<TaxonomyEvolutionResult> AnalyzeAsync(
        TaxonomyEvolutionRequest request,
        CancellationToken ct = default)
    {
        var userMessage = BuildUserMessage(request);

        _logger.LogDebug(
            "TaxonomyEvolutionAgent invoking {AgentId} for workflow {WorkflowId}",
            _agentId, request.WorkflowId);

        var response = await _runtime.InvokeAsync(
            new AgentRuntimeRequest(_agentId, _agentVersion, SystemPrompt, userMessage), ct);

        var result = ParseResponse(response.Content);

        _logger.LogInformation(
            "TaxonomyEvolutionAgent: newCategory={New}, best={Best}, confidence={Conf:P0}",
            result.NewCategorySuggested, result.BestFitExistingCategory, result.Confidence);

        return result;
    }

    private static string BuildUserMessage(TaxonomyEvolutionRequest request)
    {
        var body = request.BodyPlainText.Length > 3000
            ? request.BodyPlainText[..3000] + "\n[... truncated]"
            : request.BodyPlainText;

        var categoriesList = string.Join(", ", request.ExistingCategories);

        return $"""
            Escalation reason: {request.EscalationReason}
            Current classification: {request.CurrentCategory} (confidence: {request.CurrentConfidence:P0})

            Subject: {request.Subject}

            Body:
            {body}

            Existing document categories in the taxonomy:
            {categoriesList}

            Analyse this document and determine the best taxonomy fit.
            """;
    }

    private static TaxonomyEvolutionResult ParseResponse(string content)
    {
        var json = StripMarkdownFences(content.Trim());

        JsonElement root;
        try { root = JsonDocument.Parse(json).RootElement; }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"TaxonomyEvolutionAgent returned malformed JSON: {json}", ex);
        }

        float confidence = 0f;
        if (root.TryGetProperty("confidence", out var confProp))
            confidence = Math.Clamp((float)confProp.GetDouble(), 0f, 1f);

        bool newSuggested = false;
        if (root.TryGetProperty("newCategorySuggested", out var newProp))
            newSuggested = newProp.GetBoolean();

        return new TaxonomyEvolutionResult(
            NewCategorySuggested:      newSuggested,
            SuggestedCategory:         GetString(root, "suggestedCategory"),
            BestFitExistingCategory:   GetString(root, "bestFitExistingCategory"),
            Confidence:                confidence,
            Reasoning:                 GetString(root, "reasoning") ?? string.Empty,
            RawOutputJson:             json);
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
