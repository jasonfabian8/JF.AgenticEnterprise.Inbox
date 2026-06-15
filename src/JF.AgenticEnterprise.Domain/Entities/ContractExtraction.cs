namespace JF.AgenticEnterprise.Domain.Entities;

public class ContractExtraction
{
    public string Id { get; set; } = default!;
    public string EmailId { get; set; } = default!;
    public string AttachmentId { get; set; } = default!;
    public string AgentExecutionId { get; set; } = default!;
    public string? PartyA { get; set; }
    public float PartyAConfidence { get; set; }
    public string? PartyB { get; set; }
    public float PartyBConfidence { get; set; }
    public string? AgreementType { get; set; }
    public float AgreementTypeConfidence { get; set; }
    public string? EffectiveDate { get; set; }
    public float EffectiveDateConfidence { get; set; }
    public string? ExpiryDate { get; set; }
    public float ExpiryDateConfidence { get; set; }
    public bool? AutoRenewal { get; set; }
    public int? AutoRenewalNoticeDays { get; set; }
    public decimal? LiabilityCapAmount { get; set; }
    public string? LiabilityCapCurrency { get; set; }
    public bool? TerminationForConvenience { get; set; }
    public string? GoverningLaw { get; set; }
    public string? PaymentTerms { get; set; }
    public float OverallConfidence { get; set; }
    public string? CalculatedAlertDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Email Email { get; set; } = default!;
    public Attachment Attachment { get; set; } = default!;
    public AgentExecution AgentExecution { get; set; } = default!;
    public ICollection<RiskFlag> RiskFlags { get; set; } = [];
}
