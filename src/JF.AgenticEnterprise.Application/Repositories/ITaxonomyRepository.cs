using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Application.Repositories;

public interface ITaxonomyRepository
{
    Task<List<TaxonomyCategory>> GetAllActiveAsync(CancellationToken ct = default);
    Task<TaxonomyCategory?> GetByLabelAsync(string label, CancellationToken ct = default);
    Task<string> SaveCategoryAsync(TaxonomyCategory category, CancellationToken ct = default);
}
