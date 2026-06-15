#pragma warning disable OPENAI001

using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using JF.AgenticEnterprise.Application.Agents;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace JF.AgenticEnterprise.Infrastructure.Agents;

/// <summary>
/// Invokes Azure AI Foundry Prompt Agents via ProjectResponsesClient (Azure.AI.Extensions.OpenAI).
///
/// Auth: DefaultAzureCredential
///   - Local dev  → `az login` (Azure CLI credential is picked automatically)
///   - Azure host → Managed Identity (no extra config)
///
/// The AgentReference(name, version) pins the exact published snapshot in Foundry,
/// so a new version publish doesn't silently change behavior.
/// </summary>
public sealed class AzureAIFoundryAgentRuntime : IAgentRuntime
{
    private readonly Uri _endpoint;
    private readonly DefaultAzureCredential _credential;
    private readonly ILogger<AzureAIFoundryAgentRuntime> _logger;

    public AzureAIFoundryAgentRuntime(
        AiProviderOptions options,
        ILogger<AzureAIFoundryAgentRuntime> logger)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
            throw new InvalidOperationException(
                "AiProvider:Endpoint is required. Set it in appsettings.Development.json.");

        _endpoint   = new Uri(options.Endpoint);
        _credential = new DefaultAzureCredential();
        _logger     = logger;
    }

    /// <inheritdoc />
    public async Task<AgentRuntimeResponse> InvokeAsync(
        AgentRuntimeRequest request,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentId,      nameof(request.AgentId));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentVersion, nameof(request.AgentVersion));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserMessage,  nameof(request.UserMessage));

        var started = DateTimeOffset.UtcNow;

        _logger.LogDebug(
            "Invoking Foundry Prompt Agent {AgentId} v{Version}",
            request.AgentId, request.AgentVersion);

        // AgentReference targets the specific published version in Foundry.
        // 4th param (conversationId) is null — stateless invocation per request.
        var agentRef = new AgentReference(name: request.AgentId, version: request.AgentVersion);
        var responseClient = new ProjectResponsesClient(_endpoint, _credential, agentRef, null!, null);

        var responseOptions = new CreateResponseOptions
        {
            InputItems = { ResponseItem.CreateUserMessageItem(request.UserMessage) },
        };

        ResponseResult result;
        try
        {
            result = await responseClient.CreateResponseAsync(responseOptions, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Foundry Prompt Agent {AgentId} v{Version} failed",
                request.AgentId, request.AgentVersion);
            throw new AgentInvocationException(request.AgentId, 0, ex.Message, ex);
        }

        var content = result.GetOutputText()
            ?? throw new AgentInvocationException(
                request.AgentId, 0, "Agent returned null output text.");

        var latency = DateTimeOffset.UtcNow - started;

        _logger.LogInformation(
            "Foundry Agent {AgentId} v{Version} completed | latency: {LatencyMs}ms",
            request.AgentId, request.AgentVersion, (int)latency.TotalMilliseconds);

        return new AgentRuntimeResponse(
            Content:      content,
            FinishReason: "stop",
            Usage:        new AgentRuntimeUsage(0, 0),
            Latency:      latency);
    }
}

/// <summary>
/// Thrown when an agent invocation fails at the transport or protocol level.
/// Business-level failures (bad JSON, invalid category) throw <see cref="InvalidOperationException"/>.
/// </summary>
public sealed class AgentInvocationException : Exception
{
    public string AgentId   { get; }
    public int    HttpStatus { get; }

    public AgentInvocationException(string agentId, int httpStatus, string message, Exception? inner = null)
        : base($"Agent '{agentId}' invocation failed (HTTP {httpStatus}): {message}", inner)
    {
        AgentId    = agentId;
        HttpStatus = httpStatus;
    }
}
