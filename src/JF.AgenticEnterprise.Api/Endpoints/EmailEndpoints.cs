using JF.AgenticEnterprise.Application.DTOs;
using JF.AgenticEnterprise.Application.Orchestration;
using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Domain.Common;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace JF.AgenticEnterprise.Api.Endpoints;

public static class EmailEndpoints
{
    public static IEndpointRouteBuilder MapEmailEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/emails").WithTags("Emails");

        group.MapPost("/ingest", IngestEmail)
             .WithName("IngestEmail")
             .WithSummary("Ingest a new email into the processing queue");

        group.MapGet("/", GetEmails)
             .WithName("GetEmails")
             .WithSummary("List emails with optional filtering and pagination");

        group.MapGet("/{id}", GetEmailById)
             .WithName("GetEmailById")
             .WithSummary("Get full email detail including extraction results");

        return app;
    }

    // ── POST /api/v1/emails/ingest ────────────────────────────────────────────

    private static async Task<IResult> IngestEmail(
        [FromBody] IngestEmailRequest request,
        IEmailRepository emailRepo,
        IAuditRepository auditRepo,
        IServiceScopeFactory scopeFactory,
        CancellationToken ct)
    {
        var receivedAt = request.ReceivedAt ?? DateTimeOffset.UtcNow;
        var idempotencyKey = ComputeSha256($"{request.SenderEmail}|{request.Subject}|{receivedAt:O}");

        if (await emailRepo.ExistsByIdempotencyKeyAsync(idempotencyKey, ct))
            return Results.Conflict(new { error = "Duplicate email — already ingested." });

        var now = DateTimeOffset.UtcNow;
        var emailId = UlidGenerator.NewUlid();

        var email = new Email
        {
            Id = emailId,
            IdempotencyKey = idempotencyKey,
            Source = "MANUAL_UPLOAD",
            SenderEmail = request.SenderEmail,
            SenderName = request.SenderName ?? string.Empty,
            Subject = request.Subject,
            BodyPlainText = request.BodyPlainText,
            BodyHtml = request.BodyHtml ?? string.Empty,
            ReceivedAt = receivedAt,
            IngestedAt = now,
            Status = EmailStatus.Queued,
            CreatedAt = now,
        };

        foreach (var a in request.Attachments ?? [])
        {
            email.Attachments.Add(new Attachment
            {
                Id = UlidGenerator.NewUlid(),
                EmailId = emailId,
                Filename = a.Filename,
                MimeType = a.MimeType,
                SizeBytes = a.SizeBytes,
                StoragePath = $"attachments/{emailId}/{a.Filename}",
                CreatedAt = now,
            });
        }

        await emailRepo.SaveAsync(email, ct);

        await auditRepo.AppendAsync(new AuditEntry
        {
            Id = UlidGenerator.NewUlid(),
            EmailId = emailId,
            EntityType = nameof(Email),
            EntityId = emailId,
            ActorType = AuditActorType.System,
            ActorId = "api",
            Action = AuditAction.EmailIngested,
            OccurredAt = now,
        }, ct);

        // Trigger the agentic workflow in the background (new DI scope per execution)
        _ = Task.Run(async () =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<IWorkflowOrchestrator>();
            await orchestrator.StartForEmailAsync(emailId);
        });

        return Results.Accepted(
            $"/api/v1/emails/{emailId}",
            new IngestEmailResponse(emailId, EmailStatus.Queued, now));
    }

    // ── GET /api/v1/emails ───────────────────────────────────────────────────

    private static async Task<IResult> GetEmails(
        IEmailRepository emailRepo,
        int page = 1,
        int pageSize = 20,
        string? status = null,
        string? categoryType = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await emailRepo.GetPagedAsync(page, pageSize, status, categoryType, ct);

        var dtos = items.Select(e => new EmailListItemDto(
            Id: e.Id,
            SenderEmail: e.SenderEmail,
            SenderName: e.SenderName,
            Subject: e.Subject,
            Status: e.Status,
            CategoryType: e.Classification?.CategoryType,
            Confidence: e.Classification?.Confidence,
            AttachmentCount: e.Attachments.Count,
            ReceivedAt: e.ReceivedAt,
            ProcessedAt: e.ProcessedAt,
            ProcessingDurationMs: e.ProcessingDurationMs,
            HasConflict: e.HasConflict,
            HumanReviewed: e.HumanReviewed
        )).ToList();

        return Results.Ok(new EmailListResponse(dtos, total, page, pageSize));
    }

    // ── GET /api/v1/emails/{id} ──────────────────────────────────────────────

    private static async Task<IResult> GetEmailById(
        string id,
        IEmailRepository emailRepo,
        CancellationToken ct)
    {
        var email = await emailRepo.GetByIdAsync(id, ct);
        if (email is null) return Results.NotFound();

        var dto = new EmailDetailDto(
            Id: email.Id,
            SenderEmail: email.SenderEmail,
            SenderName: email.SenderName,
            Subject: email.Subject,
            BodyPlainText: email.BodyPlainText,
            BodyHtml: email.BodyHtml,
            Status: email.Status,
            ReceivedAt: email.ReceivedAt,
            IngestedAt: email.IngestedAt,
            ProcessedAt: email.ProcessedAt,
            ProcessingDurationMs: email.ProcessingDurationMs,
            HasConflict: email.HasConflict,
            HumanReviewed: email.HumanReviewed,
            Classification: email.Classification is null ? null : new ClassificationDto(
                email.Classification.CategoryType,
                email.Classification.Confidence,
                email.Classification.Reasoning,
                email.Classification.Source,
                email.Classification.IsOverridden),
            Attachments: email.Attachments.Select(a => new AttachmentDto(
                a.Id, a.Filename, a.MimeType, a.SizeBytes,
                a.DocumentType, a.DocumentTypeConfidence)).ToList(),
            InvoiceExtraction: email.InvoiceExtraction is null ? null : new InvoiceExtractionDto(
                email.InvoiceExtraction.VendorName,
                email.InvoiceExtraction.VendorNameConfidence,
                email.InvoiceExtraction.InvoiceNumber,
                email.InvoiceExtraction.InvoiceDate,
                email.InvoiceExtraction.DueDate,
                email.InvoiceExtraction.TotalAmount,
                email.InvoiceExtraction.TaxAmount,
                email.InvoiceExtraction.Currency,
                email.InvoiceExtraction.PoReference,
                email.InvoiceExtraction.PaymentTerms,
                email.InvoiceExtraction.ValidationStatus,
                email.InvoiceExtraction.OverallConfidence),
            ContractExtraction: email.ContractExtraction is null ? null : new ContractExtractionDto(
                email.ContractExtraction.PartyA,
                email.ContractExtraction.PartyB,
                email.ContractExtraction.AgreementType,
                email.ContractExtraction.EffectiveDate,
                email.ContractExtraction.ExpiryDate,
                email.ContractExtraction.AutoRenewal,
                email.ContractExtraction.AutoRenewalNoticeDays,
                email.ContractExtraction.LiabilityCapAmount,
                email.ContractExtraction.GoverningLaw,
                email.ContractExtraction.OverallConfidence,
                email.ContractExtraction.RiskFlags.Select(f =>
                    new RiskFlagDto(f.FlagType, f.Severity, f.Excerpt, f.Confidence)).ToList()),
            // Sprint 2 — agent-based analysis results
            InvoiceAnalysis: email.InvoiceAnalysis is null ? null : new InvoiceAnalysisDto(
                email.InvoiceAnalysis.Id,
                email.InvoiceAnalysis.Supplier,
                email.InvoiceAnalysis.InvoiceNumber,
                email.InvoiceAnalysis.InvoiceDate,
                email.InvoiceAnalysis.DueDate,
                email.InvoiceAnalysis.Currency,
                email.InvoiceAnalysis.TotalAmount,
                email.InvoiceAnalysis.Confidence,
                email.InvoiceAnalysis.Summary ?? string.Empty,
                email.InvoiceAnalysis.CreatedAt),
            ContractAnalysis: email.ContractAnalysis is null ? null
                : MapContractAnalysisDto(email.ContractAnalysis));

        return Results.Ok(dto);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ContractAnalysisDto MapContractAnalysisDto(Domain.Entities.ContractAnalysis ca)
    {
        static List<string> ParseList(string json)
        {
            try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? []; }
            catch { return []; }
        }
        return new ContractAnalysisDto(
            ca.Id, ca.ContractType,
            ParseList(ca.PartiesJson),
            ca.EffectiveDate, ca.ExpirationDate, ca.RenewalClause,
            ParseList(ca.KeyObligationsJson),
            ca.Confidence, ca.Reasoning, ca.CreatedAt);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
