using JF.AgenticEnterprise.Application.Agents;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JF.AgenticEnterprise.Infrastructure.Agents;

public sealed class InvoiceAgent : IInvoiceAgent
{
    private readonly IAgentRuntime _runtime;
    private readonly string _agentId;
    private readonly ILogger<InvoiceAgent> _logger;

    private const string SystemPrompt = """
        You are an expert invoice data extractor for a business document management system.

        Analyze the provided email content and any attachment information to extract invoice data.

        Extract the following fields (use null when the value cannot be found — do not guess):
        - supplier:       Name of the company or person issuing the invoice
        - invoiceNumber:  Invoice reference or document number
        - invoiceDate:    Date the invoice was issued (preserve original format)
        - dueDate:        Payment due date (preserve original format)
        - currency:       ISO currency code (USD, EUR, MXN, GBP, etc.)
        - totalAmount:    Total amount as a numeric value (no currency symbols or commas)
        - confidence:     Your confidence in the overall extraction, 0.0 to 1.0
        - summary:        1-2 sentences describing what this invoice is for

        Respond ONLY with valid JSON — no markdown, no explanation outside the JSON:
        {
          "supplier":      "",
          "invoiceNumber": "",
          "invoiceDate":   "",
          "dueDate":       "",
          "currency":      "",
          "totalAmount":   0,
          "confidence":    0.0,
          "summary":       ""
        }
        """;

    public InvoiceAgent(
        IAgentRuntime runtime,
        AiProviderOptions options,
        ILogger<InvoiceAgent> logger)
    {
        _runtime = runtime;
        _agentId = options.InvoiceExtractionAgentId;
        _logger = logger;
    }

    public async Task<InvoiceAnalysisResult> ExtractAsync(
        InvoiceExtractionRequest request,
        CancellationToken ct = default)
    {
        var userMessage = BuildUserMessage(request);

        _logger.LogDebug(
            "InvoiceAgent invoking {AgentId} for email {EmailId}", _agentId, request.EmailId);

        var response = await _runtime.InvokeAsync(
            new AgentRuntimeRequest(_agentId, SystemPrompt, userMessage), ct);

        var result = ParseResponse(response.Content);

        _logger.LogInformation(
            "InvoiceAgent extracted: supplier={Supplier}, invoice={Number}, confidence={Confidence:P0}",
            result.Supplier, result.InvoiceNumber, result.Confidence);

        return result;
    }

    private static string BuildUserMessage(InvoiceExtractionRequest request)
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

            Extract all available invoice fields from the above content.
            """;
    }

    private static InvoiceAnalysisResult ParseResponse(string content)
    {
        var json = StripMarkdownFences(content.Trim());

        JsonElement root;
        try { root = JsonDocument.Parse(json).RootElement; }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Invoice agent returned malformed JSON: {json}", ex);
        }

        decimal? totalAmount = null;
        if (root.TryGetProperty("totalAmount", out var amountProp) &&
            amountProp.ValueKind != JsonValueKind.Null)
        {
            totalAmount = (decimal)amountProp.GetDouble();
        }

        float confidence = 0f;
        if (root.TryGetProperty("confidence", out var confProp))
            confidence = Math.Clamp((float)confProp.GetDouble(), 0f, 1f);

        return new InvoiceAnalysisResult(
            Supplier: GetString(root, "supplier"),
            InvoiceNumber: GetString(root, "invoiceNumber"),
            InvoiceDate: GetString(root, "invoiceDate"),
            DueDate: GetString(root, "dueDate"),
            Currency: GetString(root, "currency"),
            TotalAmount: totalAmount,
            Confidence: confidence,
            Summary: GetString(root, "summary") ?? string.Empty,
            RawOutputJson: json);
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
