using JF.AgenticEnterprise.Application.Agents;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JF.AgenticEnterprise.Infrastructure.Agents;

public sealed class ContractAgent : IContractAgent
{
    private readonly IAgentRuntime _runtime;
    private readonly string _agentId;
    private readonly ILogger<ContractAgent> _logger;

    private const string SystemPrompt = """
        You are an expert contract analyst for a business document management system.

        Analyze the provided email content and any attachment information to extract contract data.

        Extract the following fields (use null when the value cannot be found — do not guess):
        - contractType:    Type of agreement (NDA, Service Agreement, MSA, SLA, Purchase Order, Lease, etc.)
        - parties:         Array of party/company names involved in the contract
        - effectiveDate:   Contract start or effective date (preserve original format)
        - expirationDate:  Contract end or expiration date (preserve original format)
        - renewalClause:   Description of auto-renewal or renewal notice terms, or null if not present
        - keyObligations:  Array of key obligations or commitments identified (max 5, concise phrases)
        - confidence:      Your confidence in the overall extraction, 0.0 to 1.0
        - reasoning:       1-2 sentences explaining the key signals found

        Respond ONLY with valid JSON — no markdown, no explanation outside the JSON:
        {
          "contractType":   "",
          "parties":        [],
          "effectiveDate":  "",
          "expirationDate": "",
          "renewalClause":  "",
          "keyObligations": [],
          "confidence":     0.0,
          "reasoning":      ""
        }
        """;

    public ContractAgent(
        IAgentRuntime runtime,
        AiProviderOptions options,
        ILogger<ContractAgent> logger)
    {
        _runtime = runtime;
        _agentId = options.ContractExtractionAgentId;
        _logger = logger;
    }

    public async Task<ContractAnalysisResult> ExtractAsync(
        ContractExtractionRequest request,
        CancellationToken ct = default)
    {
        var userMessage = BuildUserMessage(request);

        _logger.LogDebug(
            "ContractAgent invoking {AgentId} for email {EmailId}", _agentId, request.EmailId);

        var response = await _runtime.InvokeAsync(
            new AgentRuntimeRequest(_agentId, SystemPrompt, userMessage), ct);

        var result = ParseResponse(response.Content);

        _logger.LogInformation(
            "ContractAgent extracted: type={Type}, parties={Count}, confidence={Confidence:P0}",
            result.ContractType, result.Parties.Count, result.Confidence);

        return result;
    }

    private static string BuildUserMessage(ContractExtractionRequest request)
    {
        var body = request.BodyPlainText.Length > 4000
            ? request.BodyPlainText[..4000] + "\n[... truncated]"
            : request.BodyPlainText;

        var attachmentSection = request.Attachments.Count > 0
            ? string.Join("\n", request.Attachments.Select(a =>
                $"- {a.Filename} ({a.MimeType})" +
                (a.ExtractedText is not null
                    ? $"\n  Content: {(a.ExtractedText.Length > 2000 ? a.ExtractedText[..2000] + "..." : a.ExtractedText)}"
                    : "\n  [No extracted text available]")))
            : "None";

        return $"""
            Subject: {request.Subject}

            Body:
            {body}

            Attachments:
            {attachmentSection}

            Extract all available contract fields from the above content.
            """;
    }

    private static ContractAnalysisResult ParseResponse(string content)
    {
        var json = StripMarkdownFences(content.Trim());

        JsonElement root;
        try { root = JsonDocument.Parse(json).RootElement; }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Contract agent returned malformed JSON: {json}", ex);
        }

        var parties = ReadStringArray(root, "parties");
        var obligations = ReadStringArray(root, "keyObligations");

        float confidence = 0f;
        if (root.TryGetProperty("confidence", out var confProp))
            confidence = Math.Clamp((float)confProp.GetDouble(), 0f, 1f);

        return new ContractAnalysisResult(
            ContractType: GetString(root, "contractType"),
            Parties: parties,
            EffectiveDate: GetString(root, "effectiveDate"),
            ExpirationDate: GetString(root, "expirationDate"),
            RenewalClause: GetString(root, "renewalClause"),
            KeyObligations: obligations,
            Confidence: confidence,
            Reasoning: GetString(root, "reasoning") ?? string.Empty,
            RawOutputJson: json);
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var prop) || prop.ValueKind != JsonValueKind.Array)
            return [];

        return prop.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString()!)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
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
