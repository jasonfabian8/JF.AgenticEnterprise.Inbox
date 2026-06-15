namespace JF.AgenticEnterprise.Domain.Entities;

public class RiskFlag
{
    public string Id { get; set; } = default!;
    public string ContractExtractionId { get; set; } = default!;
    public string FlagType { get; set; } = default!;
    public string Severity { get; set; } = default!;
    public string Excerpt { get; set; } = string.Empty;
    public int? PageReference { get; set; }
    public float Confidence { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ContractExtraction ContractExtraction { get; set; } = default!;
}

public static class RiskFlagSeverity
{
    public const string High = "HIGH";
    public const string Medium = "MEDIUM";
    public const string Low = "LOW";
}

public static class RiskFlagType
{
    public const string AutoRenewalShortNotice = "AUTO_RENEWAL_SHORT_NOTICE";
    public const string LiabilityCapBelowThreshold = "LIABILITY_CAP_BELOW_THRESHOLD";
    public const string UncappedLiability = "UNCAPPED_LIABILITY";
    public const string BroadIndemnification = "BROAD_INDEMNIFICATION";
    public const string IpOwnershipTransfer = "IP_OWNERSHIP_TRANSFER";
    public const string NonCompeteClause = "NON_COMPETE_CLAUSE";
    public const string UnilateralTermination = "UNILATERAL_TERMINATION";
    public const string GoverningLawForeign = "GOVERNING_LAW_FOREIGN";
    public const string ExclusivityClause = "EXCLUSIVITY_CLAUSE";
}
