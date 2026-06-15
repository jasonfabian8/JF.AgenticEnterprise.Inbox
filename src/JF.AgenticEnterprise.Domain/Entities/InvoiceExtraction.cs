namespace JF.AgenticEnterprise.Domain.Entities;

public class InvoiceExtraction
{
    public string Id { get; set; } = default!;
    public string EmailId { get; set; } = default!;
    public string AttachmentId { get; set; } = default!;
    public string AgentExecutionId { get; set; } = default!;
    public string? VendorName { get; set; }
    public float VendorNameConfidence { get; set; }
    public string? InvoiceNumber { get; set; }
    public float InvoiceNumberConfidence { get; set; }
    public string? InvoiceDate { get; set; }
    public float InvoiceDateConfidence { get; set; }
    public string? DueDate { get; set; }
    public float DueDateConfidence { get; set; }
    public decimal? TotalAmount { get; set; }
    public float TotalAmountConfidence { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? Subtotal { get; set; }
    public string? Currency { get; set; }
    public string? PoReference { get; set; }
    public float PoReferenceConfidence { get; set; }
    public string? PaymentTerms { get; set; }
    public string LineItemsJson { get; set; } = "[]";
    public string ValidationStatus { get; set; } = "PASS";
    public string ValidationChecksJson { get; set; } = "[]";
    public float OverallConfidence { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Email Email { get; set; } = default!;
    public Attachment Attachment { get; set; } = default!;
    public AgentExecution AgentExecution { get; set; } = default!;
}
