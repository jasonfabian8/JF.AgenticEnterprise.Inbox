using Azure;
using Azure.AI.Inference;
using JF.AgenticEnterprise.Application.Agents;
using Microsoft.Extensions.Logging;

namespace JF.AgenticEnterprise.Infrastructure.Agents;

/// <summary>
/// Routes agent invocations to Azure AI Foundry via the Azure AI Inference SDK.
/// The project endpoint acts as the base URL; each AgentId becomes the model parameter,
/// which Foundry uses to route the request to the correct deployed agent.
/// </summary>
public sealed class AzureAIFoundryAgentRuntime : IAgentRuntime
{
    private readonly ChatCompletionsClient _client;
    private readonly ILogger<AzureAIFoundryAgentRuntime> _logger;

    public AzureAIFoundryAgentRuntime(
        AiProviderOptions options,
        ILogger<AzureAIFoundryAgentRuntime> logger)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
            throw new InvalidOperationException(
                "AiProvider:Endpoint is required for AzureAIFoundry. " +
                "Set it in appsettings.Development.json.");

        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException(
                "AiProvider:ApiKey is required for AzureAIFoundry. " +
                "Set it in appsettings.Development.json.");

        _client = new ChatCompletionsClient(
            new Uri(options.Endpoint),
            new AzureKeyCredential(options.ApiKey));

        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AgentRuntimeResponse> InvokeAsync(
        AgentRuntimeRequest request,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentId, nameof(request.AgentId));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserMessage, nameof(request.UserMessage));

        var started = DateTimeOffset.UtcNow;

        _logger.LogDebug(
            "Invoking Foundry agent {AgentId} | message length: {Len}",
            request.AgentId, request.UserMessage.Length);

        var options = new ChatCompletionsOptions
        {
            Model = request.AgentId,
            Messages =
            {
                new ChatRequestSystemMessage(request.SystemPrompt),
                new ChatRequestUserMessage(request.UserMessage),
            },
        };

        Response<ChatCompletions> response;
        try
        {
            response = await _client.CompleteAsync(options, ct);
        }
        catch (RequestFailedException rfex)
        {
            _logger.LogError(rfex,
                "Foundry agent {AgentId} returned HTTP {Status}: {Message}",
                request.AgentId, rfex.Status, rfex.Message);
            throw new AgentInvocationException(request.AgentId, rfex.Status, rfex.Message, rfex);
        }

        var latency = DateTimeOffset.UtcNow - started;
        var completions = response.Value;

        var content = completions.Content
            ?? throw new AgentInvocationException(
                request.AgentId, 0,
                "Agent returned null content. Check the agent configuration in Foundry.");

        var usage = new AgentRuntimeUsage(
            PromptTokens: completions.Usage?.PromptTokens ?? 0,
            CompletionTokens: completions.Usage?.CompletionTokens ?? 0);

        _logger.LogInformation(
            "Foundry agent {AgentId} completed | latency: {LatencyMs}ms | tokens: {Total}",
            request.AgentId, (int)latency.TotalMilliseconds, usage.TotalTokens);

        return new AgentRuntimeResponse(
            Content: content,
            FinishReason: completions.FinishReason?.ToString(),
            Usage: usage,
            Latency: latency);
    }
}

/// <summary>
/// Thrown when an agent invocation fails at the transport or protocol level.
/// Business-level failures (bad JSON, invalid category) throw <see cref="InvalidOperationException"/>.
/// </summary>
public sealed class AgentInvocationException : Exception
{
    public string AgentId { get; }
    public int HttpStatus { get; }

    public AgentInvocationException(string agentId, int httpStatus, string message, Exception? inner = null)
        : base($"Agent '{agentId}' invocation failed (HTTP {httpStatus}): {message}", inner)
    {
        AgentId = agentId;
        HttpStatus = httpStatus;
    }
}
