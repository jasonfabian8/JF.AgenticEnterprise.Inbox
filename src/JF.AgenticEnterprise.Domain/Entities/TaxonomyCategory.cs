namespace JF.AgenticEnterprise.Domain.Entities;

public class TaxonomyCategory
{
    public string Id { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public string SignalsJson { get; set; } = "[]";
    public string Routing { get; set; } = "general";
    public string SuggestedExtractionFieldsJson { get; set; } = "[]";
    public int Version { get; set; } = 1;
    public string CreatedBy { get; set; } = "SYSTEM";
    public DateTimeOffset CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public int TotalClassifiedCount { get; set; }
}
