namespace JF.AgenticEnterprise.Application.Agents;

/// <summary>
/// Generic abstraction for invoking a named AI agent and receiving a text response.
/// Implementations handle the transport details (SDK, endpoint, auth).
/// </summary>
public interface IAgentRuntime
{
    Task<AgentRuntimeResponse> InvokeAsync(
        AgentRuntimeRequest request,
        CancellationToken ct = default);
}

/// <summary>
/// Describes a single agent invocation. AgentId is the logical name of the agent
/// (e.g. "Classification-Agent") as configured in Azure AI Foundry.
/// </summary>
public sealed record AgentRuntimeRequest(
    string AgentId,
    string SystemPrompt,
    string UserMessage);

/// <summary>
/// The structured result of an agent invocation.
/// </summary>
public sealed record AgentRuntimeResponse(
    string Content,
    string? FinishReason,
    AgentRuntimeUsage Usage,
    TimeSpan Latency);

/// <summary>
/// Token consumption reported by the model.
/// </summary>
public sealed record AgentRuntimeUsage(
    int PromptTokens,
    int CompletionTokens)
{
    public int TotalTokens => PromptTokens + CompletionTokens;
}
