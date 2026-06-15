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
/// Describes a single agent invocation.
/// AgentId is the Prompt Agent name as deployed in Azure AI Foundry.
/// AgentVersion targets a specific published version (e.g. "5" for Classification-Agent v5).
/// </summary>
public sealed record AgentRuntimeRequest(
    string AgentId,
    string AgentVersion,
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
