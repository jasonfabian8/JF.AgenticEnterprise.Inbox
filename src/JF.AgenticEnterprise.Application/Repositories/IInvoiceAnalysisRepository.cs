using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Application.Repositories;

public interface IInvoiceAnalysisRepository
{
    Task<string>          SaveAsync(InvoiceAnalysis analysis, CancellationToken ct = default);
    Task<InvoiceAnalysis?> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
    Task<InvoiceAnalysis?> GetByEmailIdAsync(string emailId, CancellationToken ct = default);
}
