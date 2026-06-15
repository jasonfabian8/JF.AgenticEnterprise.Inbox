using System.Text.Json;
using JF.AgenticEnterprise.Application.Agents;
using JF.AgenticEnterprise.Application.Orchestration;
using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Application.SignalR;
using JF.AgenticEnterprise.Domain.Common;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JF.AgenticEnterprise.Infrastructure.Orchestration;

public sealed class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly IEmailRepository          _emailRepo;
    private readonly IWorkflowRepository       _workflowRepo;
    private readonly IAgentExecutionRepository _executionRepo;
    private readonly IClassificationRepository _classificationRepo;
    private readonly IClassificationAgent      _classificationAgent;
    private readonly IAgentEventBroadcaster    _broadcaster;
    private readonly ILogger<WorkflowOrchestrator> _logger;

    public WorkflowOrchestrator(
        IEmailRepository          emailRepo,
        IWorkflowRepository       workflowRepo,
        IAgentExecutionRepository executionRepo,
        IClassificationRepository classificationRepo,
        IClassificationAgent      classificationAgent,
        IAgentEventBroadcaster    broadcaster,
        ILogger<WorkflowOrchestrator> logger)
    {
        _emailRepo           = emailRepo;
        _workflowRepo        = workflowRepo;
        _executionRepo       = executionRepo;
        _classificationRepo  = classificationRepo;
        _classificationAgent = classificationAgent;
        _broadcaster         = broadcaster;
        _logger              = logger;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<string> StartForEmailAsync(string emailId, CancellationToken ct = default)
    {
        var email = await _emailRepo.GetByIdAsync(emailId, ct)
            ?? throw new InvalidOperationException($"Email {emailId} not found.");

        // Guard: only one workflow per email
        var existing = await _workflowRepo.GetByEmailIdAsync(emailId, ct);
        if (existing is not null)
        {
            _logger.LogWarning("Workflow already exists for email {EmailId}. Skipping creation.", emailId);
            return existing.Id;
        }

        var now        = DateTimeOffset.UtcNow;
        var workflowId = UlidGenerator.NewUlid();

        var workflow = new Workflow
        {
            Id          = workflowId,
            EmailId     = emailId,
            Status      = WorkflowStatus.Processing,
            CurrentStep = WorkflowStepName.Classifying,
            StartedAt   = now,
            CreatedAt   = now,
        };
        await _workflowRepo.SaveAsync(workflow, ct);

        // Mark email as processing
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

    // ── Core pipeline ─────────────────────────────────────────────────────────

    private async Task ExecuteCoreAsync(Workflow workflow, Email email, CancellationToken ct)
    {
        var now       = DateTimeOffset.UtcNow;
        var execId    = UlidGenerator.NewUlid();
        var agentName = AgentTypes.Classification;

        // 1. Create AgentExecution (RUNNING)
        var execution = new AgentExecution
        {
            Id          = execId,
            WorkflowId  = workflow.Id,
            EmailId     = email.Id,
            AgentType   = agentName,
            AgentVersion = "1.0",
            Status      = AgentExecutionStatus.Running,
            StartedAt   = now,
            CreatedAt   = now,
            InputPayloadJson = JsonSerializer.Serialize(new
            {
                subject  = email.Subject,
                bodyPreview = email.BodyPlainText.Length > 200
                    ? email.BodyPlainText[..200]
                    : email.BodyPlainText
            }),
        };
        await _executionRepo.SaveAsync(execution, ct);

        // 2. Emit agent.started
        await _broadcaster.BroadcastStartedAsync(
            new AgentStartedEvent(workflow.Id, agentName, email.Id), ct);

        _logger.LogInformation(
            "ClassificationAgent started — workflow {WorkflowId}, email {EmailId}",
            workflow.Id, email.Id);

        try
        {
            // 3. Run Classification Agent
            var startedAt = DateTimeOffset.UtcNow;
            var result    = await _classificationAgent.ClassifyAsync(
                email.Subject, email.BodyPlainText, ct);
            var durationMs = (int)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;

            // 4. Persist Classification
            var classification = new Classification
            {
                Id               = UlidGenerator.NewUlid(),
                EmailId          = email.Id,
                AgentExecutionId = execId,
                CategoryType     = result.Category,
                Confidence       = result.Confidence,
                Reasoning        = result.Reasoning,
                Source           = "AGENT",
                CreatedAt        = DateTimeOffset.UtcNow,
            };
            await _classificationRepo.SaveAsync(classification, ct);

            // 5. Update AgentExecution (COMPLETED)
            execution.Status          = AgentExecutionStatus.Completed;
            execution.ConfidenceScore = result.Confidence;
            execution.ReasoningText   = result.Reasoning;
            execution.OutputPayloadJson = JsonSerializer.Serialize(new
            {
                category   = result.Category,
                confidence = result.Confidence,
                reasoning  = result.Reasoning,
            });
            execution.DurationMs  = durationMs;
            execution.CompletedAt = DateTimeOffset.UtcNow;
            await _executionRepo.SaveAsync(execution, ct);

            // 6. Update Workflow (COMPLETED_AUTO)
            workflow.Status      = WorkflowStatus.CompletedAuto;
            workflow.CurrentStep = null;
            workflow.OutcomeType = "CLASSIFIED";
            workflow.CompletedAt = DateTimeOffset.UtcNow;
            await _workflowRepo.SaveAsync(workflow, ct);

            // 7. Update Email (COMPLETED_AUTO + ProcessedAt)
            email.Status              = EmailStatus.CompletedAuto;
            email.ProcessedAt         = DateTimeOffset.UtcNow;
            email.ProcessingDurationMs = durationMs;
            await _emailRepo.SaveAsync(email, ct);

            // 8. Emit agent.completed
            await _broadcaster.BroadcastCompletedAsync(
                new AgentCompletedEvent(
                    workflow.Id, agentName, email.Id,
                    result.Category, result.Confidence, result.Reasoning), ct);

            _logger.LogInformation(
                "ClassificationAgent completed — workflow {WorkflowId}, category {Category}, confidence {Confidence:P0}",
                workflow.Id, result.Category, result.Confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ClassificationAgent failed — workflow {WorkflowId}", workflow.Id);

            execution.Status      = AgentExecutionStatus.Failed;
            execution.ErrorMessage = ex.Message;
            execution.CompletedAt = DateTimeOffset.UtcNow;
            execution.DurationMs  = (int)(DateTimeOffset.UtcNow - now).TotalMilliseconds;
            await _executionRepo.SaveAsync(execution, ct);

            workflow.Status      = WorkflowStatus.Failed;
            workflow.CurrentStep = null;
            workflow.CompletedAt = DateTimeOffset.UtcNow;
            await _workflowRepo.SaveAsync(workflow, ct);

            email.Status = EmailStatus.Failed;
            await _emailRepo.SaveAsync(email, ct);

            await _broadcaster.BroadcastFailedAsync(
                new AgentFailedEvent(workflow.Id, agentName, email.Id, ex.Message), ct);
        }
    }
}
