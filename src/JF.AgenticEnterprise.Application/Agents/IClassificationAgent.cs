namespace JF.AgenticEnterprise.Application.Agents;

public interface IClassificationAgent
{
    Task<ClassificationResult> ClassifyAsync(
        string subject,
        string bodyPlainText,
        CancellationToken ct = default);
}
