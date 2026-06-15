using JF.AgenticEnterprise.Application.Agents;
using JF.AgenticEnterprise.Application.Orchestration;
using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Application.Services;
using JF.AgenticEnterprise.Application.SignalR;
using JF.AgenticEnterprise.Domain.Common;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JF.AgenticEnterprise.Infrastructure.Orchestration;

public sealed class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly IEmailRepository _emailRepo;
    private readonly IWorkflowRepository _workflowRepo;
    private readonly IAgentExecutionRepository _executionRepo;
    private readonly IClassificationRepository _classificationRepo;
    private readonly IOrchestrationDecisionRepository _orchestrationRepo;
    private readonly IInvoiceAnalysisRepository _invoiceAnalysisRepo;
    private readonly IContractAnalysisRepository _contractAnalysisRepo;
    private readonly IWorkflowResultRepository _workflowResultRepo;
    private readonly IClassificationAgent _classificationAgent;
    private readonly IOrchestratorAgent _orchestratorAgent;
    private readonly IInvoiceAgent _invoiceAgent;
    private readonly IContractAgent _contractAgent;
    private readonly IDocumentExtractionService _documentExtractor;
    private readonly IAgentEventBroadcaster _broadcaster;
    private readonly ILogger<WorkflowOrchestrator> _logger;

    public WorkflowOrchestrator(
        IEmailRepository emailRepo,
        IWorkflowRepository workflowRepo,
        IAgentExecutionRepository executionRepo,
        IClassificationRepository classificationRepo,
        IOrchestrationDecisionRepository orchestrationRepo,
        IInvoiceAnalysisRepository invoiceAnalysisRepo,
        IContractAnalysisRepository contractAnalysisRepo,
        IWorkflowResultRepository workflowResultRepo,
        IClassificationAgent classificationAgent,
        IOrchestratorAgent orchestratorAgent,
        IInvoiceAgent invoiceAgent,
        IContractAgent contractAgent,
        IDocumentExtractionService documentExtractor,
        IAgentEventBroadcaster broadcaster,
        ILogger<WorkflowOrchestrator> logger)
    {
        _emailRepo = emailRepo;
        _workflowRepo = workflowRepo;
        _executionRepo = executionRepo;
        _classificationRepo = classificationRepo;
        _orchestrationRepo = orchestrationRepo;
        _invoiceAnalysisRepo = invoiceAnalysisRepo;
        _contractAnalysisRepo = contractAnalysisRepo;
        _workflowResultRepo = workflowResultRepo;
        _classificationAgent = classificationAgent;
        _orchestratorAgent = orchestratorAgent;
        _invoiceAgent = invoiceAgent;
        _contractAgent = contractAgent;
        _documentExtractor = documentExtractor;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<string> StartForEmailAsync(string emailId, CancellationToken ct = default)
    {
        var email = await _emailRepo.GetByIdAsync(emailId, ct)
            ?? throw new InvalidOperationException($"Email {emailId} not found.");

        var existing = await _workflowRepo.GetByEmailIdAsync(emailId, ct);
        if (existing is not null)
        {
            _logger.LogWarning("Workflow already exists for email {EmailId}. Skipping.", emailId);
            return existing.Id;
        }

        var now = DateTimeOffset.UtcNow;
        var workflowId = UlidGenerator.NewUlid();

        var workflow = new Workflow
        {
            Id = workflowId,
            EmailId = emailId,
            Status = WorkflowStatus.Processing,
            CurrentStep = WorkflowStepName.Classifying,
            StartedAt = now,
            CreatedAt = now,
        };
        await _workflowRepo.SaveAsync(workflow, ct);

        email.Status = EmailStatus.Processing;
        await _emailRepo.SaveAsync(email, ct);

        await ExecuteCoreAsync(workflow, email, ct);
        return workflowId;
    }

    public async Task ExecuteAsync(string workflowId, CancellationToken ct = default)
    {
        var workflow = await _workflowRepo.GetByIdAsync(workflowId, ct)
            ?? throw new InvalidOperationException($"Workflow {workflowId} not found.");

        var email = await _emailRepo.GetByIdAsync(workflow.EmailId, ct)
            ?? throw new InvalidOperationException($"Email {workflow.EmailId} not found.");

        await ExecuteCoreAsync(workflow, email, ct);
    }

    // ── Pipeline ──────────────────────────────────────────────────────────────

    private async Task ExecuteCoreAsync(Workflow workflow, Email email, CancellationToken ct)
    {
        // Pre-extract document text once — shared across all specialized agents.
        var attachmentContexts = await BuildAttachmentContextsAsync(email, ct);

        // ═══ PHASE 1: CLASSIFICATION ══════════════════════════════════════════
        ClassificationResult classResult;
        string classExecId;
        try
        {
            (classResult, classExecId) = await RunClassificationAsync(workflow, email, ct);
        }
        catch (Exception ex)
        {
            await FailWorkflowAsync(workflow, email, AgentTypes.Classification, null, ex, ct);
            return;
        }

        // Advance to orchestration step
        workflow.CurrentStep = WorkflowStepName.Orchestrating;
        await _workflowRepo.SaveAsync(workflow, ct);
        await _broadcaster.BroadcastWorkflowUpdatedAsync(new WorkflowUpdatedEvent(
            workflow.Id, email.Id, workflow.Status,
            WorkflowStepName.Orchestrating, null,
            DateTimeOffset.UtcNow), ct);

        // ═══ PHASE 2: ORCHESTRATION ═══════════════════════════════════════════
        OrchestratorResult orchResult;
        try
        {
            orchResult = await RunOrchestrationAsync(workflow, email, classResult, ct);
        }
        catch (Exception ex)
        {
            await FailWorkflowAsync(workflow, email, AgentTypes.Orchestrator, null, ex, ct);
            return;
        }

        // Advance to specialized step
        var nextStepName = MapNextAgentToStepName(orchResult.NextAgent);
        workflow.CurrentStep = nextStepName;
        await _workflowRepo.SaveAsync(workflow, ct);
        await _broadcaster.BroadcastWorkflowUpdatedAsync(new WorkflowUpdatedEvent(
            workflow.Id, email.Id, workflow.Status,
            nextStepName, orchResult.NextAgent,
            DateTimeOffset.UtcNow), ct);

        // ═══ PHASE 3: SPECIALIZED AGENT ═══════════════════════════════════════
        string? invoiceAnalysisId = null;
        string? contractAnalysisId = null;

        if (orchResult.NextAgent == NextAgentName.InvoiceAgent)
        {
            try
            {
                invoiceAnalysisId = await RunInvoiceAgentAsync(
                    workflow, email, attachmentContexts, ct);
            }
            catch (Exception ex)
            {
                await FailWorkflowAsync(workflow, email, AgentTypes.Invoice, null, ex, ct);
                return;
            }
        }
        else if (orchResult.NextAgent == NextAgentName.ContractAgent)
        {
            try
            {
                contractAnalysisId = await RunContractAgentAsync(
                    workflow, email, attachmentContexts, ct);
            }
            catch (Exception ex)
            {
                await FailWorkflowAsync(workflow, email, AgentTypes.Contract, null, ex, ct);
                return;
            }
        }

        // ═══ PHASE 4: FINALIZE ════════════════════════════════════════════════
        await FinalizeAsync(
            workflow, email, classResult, orchResult,
            invoiceAnalysisId, contractAnalysisId, ct);
    }

    // ── Phase implementations ─────────────────────────────────────────────────

    private async Task<(ClassificationResult result, string execId)> RunClassificationAsync(
        Workflow workflow, Email email, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var execId = UlidGenerator.NewUlid();

        var execution = CreateExecution(execId, workflow.Id, email.Id,
            AgentTypes.Classification, now,
            JsonSerializer.Serialize(new
            {
                subject = email.Subject,
                bodyPreview = email.BodyPlainText.Length > 200
                    ? email.BodyPlainText[..200]
                    : email.BodyPlainText,
            }));

        await _executionRepo.SaveAsync(execution, ct);
        await _broadcaster.BroadcastStartedAsync(new AgentStartedEvent(
            workflow.Id, AgentTypes.Classification, email.Id, DateTimeOffset.UtcNow), ct);

        _logger.LogInformation("ClassificationAgent started — workflow {WorkflowId}", workflow.Id);

        var start = DateTimeOffset.UtcNow;
        var result = await _classificationAgent.ClassifyAsync(
            email.Subject, email.BodyPlainText, ct);
        var durationMs = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;

        // Persist classification
        await _classificationRepo.SaveAsync(new Classification
        {
            Id = UlidGenerator.NewUlid(),
            EmailId = email.Id,
            AgentExecutionId = execId,
            CategoryType = result.Category,
            Confidence = result.Confidence,
            Reasoning = result.Reasoning,
            Source = "AGENT",
            CreatedAt = DateTimeOffset.UtcNow,
        }, ct);

        // Complete execution
        execution.Status = AgentExecutionStatus.Completed;
        execution.ConfidenceScore = result.Confidence;
        execution.ReasoningText = result.Reasoning;
        execution.DurationMs = durationMs;
        execution.CompletedAt = DateTimeOffset.UtcNow;
        execution.OutputPayloadJson = JsonSerializer.Serialize(new
        {
            category = result.Category,
            confidence = result.Confidence,
            reasoning = result.Reasoning,
        });
        await _executionRepo.SaveAsync(execution, ct);

        await _broadcaster.BroadcastCompletedAsync(new AgentCompletedEvent(
            workflow.Id, AgentTypes.Classification, email.Id,
            result.Category, result.Confidence, result.Reasoning,
            DateTimeOffset.UtcNow), ct);

        _logger.LogInformation(
            "ClassificationAgent completed — category={Category}, confidence={Confidence:P0}",
            result.Category, result.Confidence);

        return (result, execId);
    }

    private async Task<OrchestratorResult> RunOrchestrationAsync(
        Workflow workflow, Email email,
        ClassificationResult classResult, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var execId = UlidGenerator.NewUlid();

        var execution = CreateExecution(execId, workflow.Id, email.Id,
            AgentTypes.Orchestrator, now,
            JsonSerializer.Serialize(new
            {
                category = classResult.Category,
                confidence = classResult.Confidence,
            }));

        await _executionRepo.SaveAsync(execution, ct);
        await _broadcaster.BroadcastStartedAsync(new AgentStartedEvent(
            workflow.Id, AgentTypes.Orchestrator, email.Id, DateTimeOffset.UtcNow), ct);

        _logger.LogInformation(
            "OrchestratorAgent started — workflow {WorkflowId}, category {Category}",
            workflow.Id, classResult.Category);

        var start = DateTimeOffset.UtcNow;
        var result = await _orchestratorAgent.DecideAsync(new OrchestratorRequest(
            workflow.Id, email.Id,
            classResult.Category, classResult.Confidence, classResult.Reasoning), ct);
        var durationMs = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;

        // Persist orchestration decision
        await _orchestrationRepo.SaveAsync(new OrchestrationDecision
        {
            Id = UlidGenerator.NewUlid(),
            WorkflowId = workflow.Id,
            AgentExecutionId = execId,
            ClassificationCategory = classResult.Category,
            NextAgent = result.NextAgent,
            WorkflowStatus = result.WorkflowStatus,
            Reasoning = result.Reasoning,
            DecidedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        }, ct);

        execution.Status = AgentExecutionStatus.Completed;
        execution.DurationMs = durationMs;
        execution.CompletedAt = DateTimeOffset.UtcNow;
        execution.OutputPayloadJson = JsonSerializer.Serialize(new
        {
            nextAgent = result.NextAgent,
            workflowStatus = result.WorkflowStatus,
            reasoning = result.Reasoning,
        });
        await _executionRepo.SaveAsync(execution, ct);

        await _broadcaster.BroadcastCompletedAsync(new AgentCompletedEvent(
            workflow.Id, AgentTypes.Orchestrator, email.Id,
            result.NextAgent, 1f, result.Reasoning,
            DateTimeOffset.UtcNow), ct);

        _logger.LogInformation(
            "OrchestratorAgent completed — nextAgent={NextAgent}", result.NextAgent);

        return result;
    }

    private async Task<string> RunInvoiceAgentAsync(
        Workflow workflow, Email email,
        IReadOnlyList<AttachmentContext> attachments, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var execId = UlidGenerator.NewUlid();

        var execution = CreateExecution(execId, workflow.Id, email.Id,
            AgentTypes.Invoice, now, null);

        await _executionRepo.SaveAsync(execution, ct);
        await _broadcaster.BroadcastStartedAsync(new AgentStartedEvent(
            workflow.Id, AgentTypes.Invoice, email.Id, DateTimeOffset.UtcNow), ct);

        _logger.LogInformation("InvoiceAgent started — workflow {WorkflowId}", workflow.Id);

        var start = DateTimeOffset.UtcNow;
        var result = await _invoiceAgent.ExtractAsync(new InvoiceExtractionRequest(
            workflow.Id, email.Id, email.Subject, email.BodyPlainText, attachments), ct);
        var durationMs = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;

        var analysisId = UlidGenerator.NewUlid();
        await _invoiceAnalysisRepo.SaveAsync(new InvoiceAnalysis
        {
            Id = analysisId,
            EmailId = email.Id,
            WorkflowId = workflow.Id,
            AgentExecutionId = execId,
            Supplier = result.Supplier,
            InvoiceNumber = result.InvoiceNumber,
            InvoiceDate = result.InvoiceDate,
            DueDate = result.DueDate,
            Currency = result.Currency,
            TotalAmount = result.TotalAmount,
            Summary = result.Summary,
            Confidence = result.Confidence,
            RawOutputJson = result.RawOutputJson,
            CreatedAt = DateTimeOffset.UtcNow,
        }, ct);

        execution.Status = AgentExecutionStatus.Completed;
        execution.ConfidenceScore = result.Confidence;
        execution.DurationMs = durationMs;
        execution.CompletedAt = DateTimeOffset.UtcNow;
        execution.OutputPayloadJson = result.RawOutputJson;
        await _executionRepo.SaveAsync(execution, ct);

        await _broadcaster.BroadcastCompletedAsync(new AgentCompletedEvent(
            workflow.Id, AgentTypes.Invoice, email.Id,
            result.Supplier ?? "Invoice",
            result.Confidence,
            result.Summary,
            DateTimeOffset.UtcNow), ct);

        _logger.LogInformation(
            "InvoiceAgent completed — supplier={Supplier}, confidence={Confidence:P0}",
            result.Supplier, result.Confidence);

        return analysisId;
    }

    private async Task<string> RunContractAgentAsync(
        Workflow workflow, Email email,
        IReadOnlyList<AttachmentContext> attachments, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var execId = UlidGenerator.NewUlid();

        var execution = CreateExecution(execId, workflow.Id, email.Id,
            AgentTypes.Contract, now, null);

        await _executionRepo.SaveAsync(execution, ct);
        await _broadcaster.BroadcastStartedAsync(new AgentStartedEvent(
            workflow.Id, AgentTypes.Contract, email.Id, DateTimeOffset.UtcNow), ct);

        _logger.LogInformation("ContractAgent started — workflow {WorkflowId}", workflow.Id);

        var start = DateTimeOffset.UtcNow;
        var result = await _contractAgent.ExtractAsync(new ContractExtractionRequest(
            workflow.Id, email.Id, email.Subject, email.BodyPlainText, attachments), ct);
        var durationMs = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;

        var analysisId = UlidGenerator.NewUlid();
        await _contractAnalysisRepo.SaveAsync(new ContractAnalysis
        {
            Id = analysisId,
            EmailId = email.Id,
            WorkflowId = workflow.Id,
            AgentExecutionId = execId,
            ContractType = result.ContractType,
            PartiesJson = JsonSerializer.Serialize(result.Parties),
            EffectiveDate = result.EffectiveDate,
            ExpirationDate = result.ExpirationDate,
            RenewalClause = result.RenewalClause,
            KeyObligationsJson = JsonSerializer.Serialize(result.KeyObligations),
            Confidence = result.Confidence,
            Reasoning = result.Reasoning,
            RawOutputJson = result.RawOutputJson,
            CreatedAt = DateTimeOffset.UtcNow,
        }, ct);

        execution.Status = AgentExecutionStatus.Completed;
        execution.ConfidenceScore = result.Confidence;
        execution.DurationMs = durationMs;
        execution.CompletedAt = DateTimeOffset.UtcNow;
        execution.OutputPayloadJson = result.RawOutputJson;
        await _executionRepo.SaveAsync(execution, ct);

        await _broadcaster.BroadcastCompletedAsync(new AgentCompletedEvent(
            workflow.Id, AgentTypes.Contract, email.Id,
            result.ContractType ?? "Contract",
            result.Confidence,
            result.Reasoning,
            DateTimeOffset.UtcNow), ct);

        _logger.LogInformation(
            "ContractAgent completed — type={Type}, confidence={Confidence:P0}",
            result.ContractType, result.Confidence);

        return analysisId;
    }

    private async Task FinalizeAsync(
        Workflow workflow, Email email,
        ClassificationResult classResult,
        OrchestratorResult orchResult,
        string? invoiceAnalysisId,
        string? contractAnalysisId,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var finalWorkflowStatus = orchResult.NextAgent == NextAgentName.HumanReview
            ? WorkflowStatus.AwaitingReview
            : WorkflowStatus.CompletedAuto;

        var finalEmailStatus = orchResult.NextAgent == NextAgentName.HumanReview
            ? EmailStatus.AwaitingReview
            : EmailStatus.CompletedAuto;

        var summary = orchResult.NextAgent switch
        {
            NextAgentName.InvoiceAgent => $"Invoice extracted. Classified as {classResult.Category}.",
            NextAgentName.ContractAgent => $"Contract analysed. Classified as {classResult.Category}.",
            NextAgentName.HumanReview => $"Queued for human review. Category: {classResult.Category}.",
            _ => $"Completed without extraction. Category: {classResult.Category}.",
        };

        var resultStatus = orchResult.NextAgent is NextAgentName.InvoiceAgent or NextAgentName.ContractAgent
            ? WorkflowResultStatus.CompletedExtracted
            : orchResult.NextAgent == NextAgentName.HumanReview
                ? WorkflowResultStatus.AwaitingReview
                : WorkflowResultStatus.Completed;

        // Persist WorkflowResult aggregate
        await _workflowResultRepo.SaveAsync(new WorkflowResult
        {
            Id = UlidGenerator.NewUlid(),
            WorkflowId = workflow.Id,
            ClassificationCategory = classResult.Category,
            ClassificationConfidence = classResult.Confidence,
            RoutedToAgent = orchResult.NextAgent,
            InvoiceAnalysisId = invoiceAnalysisId,
            ContractAnalysisId = contractAnalysisId,
            FinalStatus = resultStatus,
            Summary = summary,
            CompletedAt = now,
            CreatedAt = now,
        }, ct);

        workflow.Status = finalWorkflowStatus;
        workflow.CurrentStep = null;
        workflow.OutcomeType = orchResult.NextAgent;
        workflow.CompletedAt = now;
        await _workflowRepo.SaveAsync(workflow, ct);

        email.Status = finalEmailStatus;
        email.ProcessedAt = now;
        await _emailRepo.SaveAsync(email, ct);

        await _broadcaster.BroadcastWorkflowCompletedAsync(new WorkflowCompletedEvent(
            workflow.Id, email.Id,
            finalWorkflowStatus,
            classResult.Category,
            orchResult.NextAgent,
            invoiceAnalysisId,
            contractAnalysisId,
            now), ct);

        _logger.LogInformation(
            "Workflow {WorkflowId} completed — status={Status}, routedTo={NextAgent}",
            workflow.Id, finalWorkflowStatus, orchResult.NextAgent);
    }

    private async Task FailWorkflowAsync(
        Workflow workflow,
        Email email,
        string agentType,
        string? execId,
        Exception ex,
        CancellationToken ct)
    {
        _logger.LogError(ex,
            "{AgentType} failed — workflow {WorkflowId}", agentType, workflow.Id);

        if (execId is not null)
        {
            // Best-effort: update the execution record if we have its id
            var execution = await _executionRepo.GetByIdAsync(execId, ct);
            if (execution is not null)
            {
                execution.Status = AgentExecutionStatus.Failed;
                execution.ErrorMessage = ex.Message;
                execution.CompletedAt = DateTimeOffset.UtcNow;
                await _executionRepo.SaveAsync(execution, ct);
            }
        }

        workflow.Status = WorkflowStatus.Failed;
        workflow.CurrentStep = null;
        workflow.CompletedAt = DateTimeOffset.UtcNow;
        await _workflowRepo.SaveAsync(workflow, ct);

        email.Status = EmailStatus.Failed;
        await _emailRepo.SaveAsync(email, ct);

        await _broadcaster.BroadcastFailedAsync(new AgentFailedEvent(
            workflow.Id, agentType, email.Id, ex.Message, DateTimeOffset.UtcNow), ct);

        await _broadcaster.BroadcastWorkflowCompletedAsync(new WorkflowCompletedEvent(
            workflow.Id, email.Id,
            WorkflowStatus.Failed,
            string.Empty, agentType,
            null, null,
            DateTimeOffset.UtcNow), ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AgentExecution CreateExecution(
        string execId, string workflowId, string emailId,
        string agentType, DateTimeOffset now, string? inputJson)
        => new()
        {
            Id = execId,
            WorkflowId = workflowId,
            EmailId = emailId,
            AgentType = agentType,
            AgentVersion = "2.0",
            Status = AgentExecutionStatus.Running,
            InputPayloadJson = inputJson,
            StartedAt = now,
            CreatedAt = now,
        };

    private static string MapNextAgentToStepName(string nextAgent) => nextAgent switch
    {
        NextAgentName.InvoiceAgent => WorkflowStepName.ExtractingInvoice,
        NextAgentName.ContractAgent => WorkflowStepName.ExtractingContract,
        NextAgentName.HumanReview => WorkflowStepName.HumanReview,
        _ => WorkflowStepName.Completing,
    };

    private async Task<IReadOnlyList<AttachmentContext>> BuildAttachmentContextsAsync(
        Email email, CancellationToken ct)
    {
        if (!email.Attachments.Any())
            return [];

        var extractionRequest = new DocumentExtractionRequest(
            email.Id,
            email.Attachments.Select(a => new AttachmentExtractionItem(
                a.Id, a.Filename, a.MimeType, a.StoragePath, a.ExtractedText)).ToList());

        var extracted = await _documentExtractor.ExtractAsync(extractionRequest, ct);

        return extracted.Results
            .Select(r => new AttachmentContext(r.Filename, r.MimeType, r.ExtractedText))
            .ToList();
    }
}
