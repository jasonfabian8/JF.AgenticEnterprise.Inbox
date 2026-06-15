namespace JF.AgenticEnterprise.Application.Agents;

public interface IContractAgent
{
    Task<ContractAnalysisResult> ExtractAsync(
        ContractExtractionRequest request,
        CancellationToken         ct = default);
}

public sealed record ContractExtractionRequest(
    string WorkflowId,
    string EmailId,
    string Subject,
    string BodyPlainText,
    IReadOnlyList<AttachmentContext> Attachments);

public sealed record ContractAnalysisResult(
    string?            ContractType,
    IReadOnlyList<string> Parties,
    string?            EffectiveDate,
    string?            ExpirationDate,
    string?            RenewalClause,
    IReadOnlyList<string> KeyObligations,
    float              Confidence,
    string             Reasoning,
    string             RawOutputJson);
