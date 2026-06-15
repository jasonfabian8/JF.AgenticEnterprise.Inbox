using System.Text.Json;
using JF.AgenticEnterprise.Application.Agents;
using Microsoft.Extensions.Logging;

namespace JF.AgenticEnterprise.Infrastructure.Agents;

/// <summary>
/// Implements <see cref="IClassificationAgent"/> by delegating to <see cref="IAgentRuntime"/>.
/// All prompt engineering and JSON parsing is encapsulated here; the runtime handles transport.
/// </summary>
public sealed class ClassificationAgent : IClassificationAgent
{
    private readonly IAgentRuntime                _runtime;
    private readonly string                       _agentId;
    private readonly ILogger<ClassificationAgent> _logger;

    private const string SystemPrompt = """
        You are an expert business email classifier working for a document management system.

        Analyze the email and classify it into EXACTLY ONE of these categories:
        - Invoice: Bills, payment requests, vendor invoices, receipts, purchase orders
        - Contract: Legal agreements, NDAs, service contracts, MSAs, terms and conditions
        - Commercial Proposal: Quotes, proposals, RFP responses, bids, offers
        - Information Request: Questions, support requests, inquiries, follow-ups
        - Marketing: Promotions, newsletters, advertisements, announcements
        - Bank Statement: Account statements, bank notifications, transaction reports
        - Unknown: Cannot be clearly classified into any above category

        Rules:
        1. Choose the single best matching category.
        2. Confidence 0.9+ means you are very certain; below 0.6 means ambiguous.
        3. Reasoning must be 1-2 concise sentences explaining the key signals you found.
        4. If the email is empty or has no meaningful content, use Unknown with low confidence.

        Respond ONLY with valid JSON — no markdown, no explanation outside the JSON:
        {
          "category": "<exact category name from the list above>",
          "confidence": <number between 0.0 and 1.0>,
          "reasoning": "<1-2 sentence explanation>"
        }
        """;

    public ClassificationAgent(
        IAgentRuntime                 runtime,
        AiProviderOptions             options,
        ILogger<ClassificationAgent>  logger)
    {
        _runtime = runtime;
        _agentId = options.ClassificationAgentId;
        _logger  = logger;
    }

    /// <inheritdoc />
    public async Task<ClassificationResult> ClassifyAsync(
        string            subject,
        string            bodyPlainText,
        CancellationToken ct = default)
    {
        var userMessage = BuildUserMessage(subject, bodyPlainText);

        var request = new AgentRuntimeRequest(
            AgentId:      _agentId,
            SystemPrompt: SystemPrompt,
            UserMessage:  userMessage);

        _logger.LogDebug(
            "ClassificationAgent invoking agent {AgentId} for subject: {Subject}",
            _agentId, subject);

        var response = await _runtime.InvokeAsync(request, ct);

        _logger.LogDebug(
            "ClassificationAgent received {Tokens} tokens in {Latency}ms",
            response.Usage.TotalTokens, (int)response.Latency.TotalMilliseconds);

        return ParseResponse(response.Content);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildUserMessage(string subject, string body)
    {
        var truncated = body.Length > 4000
            ? body[..4000] + "\n[... truncated]"
            : body;

        return $"Subject: {subject}\n\nBody:\n{truncated}";
    }

    private static ClassificationResult ParseResponse(string content)
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
                $"Agent returned malformed JSON. Raw content: {json}", ex);
        }

        if (!root.TryGetProperty("category",   out var catProp)   ||
            !root.TryGetProperty("confidence",  out var confProp)  ||
            !root.TryGetProperty("reasoning",   out var reasonProp))
        {
            throw new InvalidOperationException(
                $"Agent response is missing required fields (category/confidence/reasoning). Raw: {json}");
        }

        var category   = catProp.GetString()            ?? EmailCategory.Unknown;
        var confidence = (float)confProp.GetDouble();
        var reasoning  = reasonProp.GetString()         ?? string.Empty;

        // Normalise hallucinated categories to Unknown
        if (!EmailCategory.All.Contains(category, StringComparer.OrdinalIgnoreCase))
        {
            category   = EmailCategory.Unknown;
            confidence = Math.Min(confidence, 0.3f);
        }

        confidence = Math.Clamp(confidence, 0f, 1f);

        return new ClassificationResult(category, confidence, reasoning);
    }

    private static string StripMarkdownFences(string text)
    {
        if (!text.StartsWith("```", StringComparison.Ordinal))
            return text;

        var newline = text.IndexOf('\n');
        if (newline < 0) return text;

        var end = text.LastIndexOf("```", StringComparison.Ordinal);
        if (end <= newline) return text;

        return text[(newline + 1)..end].Trim();
    }
}
