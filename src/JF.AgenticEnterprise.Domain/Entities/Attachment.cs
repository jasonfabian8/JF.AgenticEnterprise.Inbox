namespace JF.AgenticEnterprise.Domain.Entities;

public class Attachment
{
    public string Id { get; set; } = default!;
    public string EmailId { get; set; } = default!;
    public string Filename { get; set; } = default!;
    public string MimeType { get; set; } = default!;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = default!;
    public string? ExtractedText { get; set; }
    public string? DocumentType { get; set; }
    public float DocumentTypeConfidence { get; set; }
    public string OcrStatus { get; set; } = "NOT_REQUIRED";
    public DateTimeOffset CreatedAt { get; set; }

    public Email Email { get; set; } = default!;
}
