using JF.AgenticEnterprise.Application.Agents;
using JF.AgenticEnterprise.Application.Orchestration;
using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Application.Services;
using JF.AgenticEnterprise.Application.SignalR;
using JF.AgenticEnterprise.Domain.Common;
using JF.AgenticEnterprise.Domain.Entities;
using JF.AgenticEnterprise.Domain.Settings;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JF.AgenticEnterprise.Infrastructure.Orchestration;

public sealed class WorkflowOrchestrator : IWorkflowOrchestrator
{
    // ── Sprint 1 / 2 dependencies ─────────────────────────────────────────────
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

    // ── Sprint 3 dependencies ─────────────────────────────────────────────────
    private readonly ITaxonomyEvolutionAgent _taxonomyEvolutionAgent;
    private readonly IHumanCollaborationAgent _humanCollaborationAgent;
    private readonly IConflictDetectionService _conflictDetector;
    private readonly IAgentConflictRepository _conflictRepo;
    private readonly IWorkflowKnowledgeRepository _knowledgeRepo;
    private readonly IHumanReviewRepository _reviewRepo;
    private readonly ITaxonomyProposalRepository _proposalRepo;
    private readonly ITaxonomyRepository _taxonomyRepo;
    private readonly WorkflowSettings _settings;

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
        ITaxonomyEvolutionAgent taxonomyEvolutionAgent,
        IHumanCollaborationAgent humanCollaborationAgent,
        IConflictDetectionService conflictDetector,
        IAgentConflictRepository conflictRepo,
        IWorkflowKnowledgeRepository knowledgeRepo,
        IHumanReviewRepository reviewRepo,
        ITaxonomyProposalRepository proposalRepo,
        ITaxonomyRepository taxonomyRepo,
        WorkflowSettings settings,
        ILogger<WorkflowOrchestrator> logger)
    {
        _emailRepo             = emailRepo;
        _workflowRepo          = workflowRepo;
        _executionRepo         = executionRepo;
        _classificationRepo    = classificationRepo;
        _orchestrationRepo     = orchestrationRepo;
        _invoiceAnalysisRepo   = invoiceAnalysisRepo;
        _contractAnalysisRepo  = contractAnalysisRepo;
        _workflowResultRepo    = workflowResultRepo;
        _classificationAgent   = classificationAgent;
        _orchestratorAgent     = orchestratorAgent;
        _invoiceAgent          = invoiceAgent;
        _contractAgent         = contractAgent;
        _documentExtractor     = documentExtractor;
        _broadcaster           = broadcaster;
        _taxonomyEvolutionAgent  = taxonomyEvolutionAgent;
        _humanCollaborationAgent = humanCollaborationAgent;
        _conflictDetector      = conflictDetector;
        _conflictRepo          = conflictRepo;
        _knowledgeRepo         = knowledgeRepo;
        _reviewRepo            = reviewRepo;
        _proposalRepo          = proposalRepo;
        _taxonomyRepo          = taxonomyRepo;
        _settings              = settings;
        _logger                = logger;
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
        var attachmentContexts = await BuildAttachmentContextsAsync(email, ct);

        // ═══ PHASE 1: CLASSIFICATION ══════════════════════════════════════════
        ClassificationResult classResult;
        try
        {
            classResult = await RunClassificationAsync(workflow, email, ct);
        }
        catch (Exception ex)
        {
            await FailWorkflowAsync(workflow, email, AgentTypes.Classification, null, ex, ct);
            return;
        }

        // Initialize WorkflowKnowledge — tracks how understanding evolves.
        var knowledge = await InitializeKnowledgeAsync(workflow, email, classResult, ct);

        // ── Confidence check after classification ─────────────────────────────
        var band = _settings.GetBand(classResult.Confidence);
        AgentConflict? lowConfidenceConflict = null;

        if (band == ConfidenceBand.Low)
        {
            lowConfidenceConflict = _conflictDetector.DetectLowConfidence(
                workflow.Id, email.Id,
                AgentTypes.Classification, classResult.Category,
                classResult.Confidence, _settings);

            if (lowConfidenceConflict is not null)
                await SaveAndBroadcastConflictAsync(lowConfidenceConflict, email, ct);
        }

        // ═══ PHASE 2: ORCHESTRATION ═══════════════════════════════════════════
        workflow.CurrentStep = WorkflowStepName.Orchestrating;
        await _workflowRepo.SaveAsync(workflow, ct);
        await _broadcaster.BroadcastWorkflowUpdatedAsync(new WorkflowUpdatedEvent(
            workflow.Id, email.Id, workflow.Status,
            WorkflowStepName.Orchestrating, null,
            DateTimeOffset.UtcNow), ct);

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
        AgentConflict? categoryConflict = null;

        // When the orchestrator routes directly to Human Review (no specialized agent),
        // create a HumanReview record immediately so it appears in the queue.
        if (orchResult.NextAgent == NextAgentName.HumanReview)
        {
            var review = new HumanReview
            {
                Id              = UlidGenerator.NewUlid(),
                EmailId         = email.Id,
                WorkflowId      = workflow.Id,
                ReviewType      = "CLASSIFICATION_REVIEW",
                Priority        = ReviewPriority.Normal,
                Status          = ReviewStatus.Pending,
                Reason          = orchResult.Reasoning,
                AgentConfidence = classResult.Confidence,
                ConflictId      = null,
                QueuedAt        = DateTimeOffset.UtcNow,
                CreatedAt       = DateTimeOffset.UtcNow,
            };
            await _reviewRepo.SaveAsync(review, ct);

            await _broadcaster.BroadcastReviewRequestedAsync(new ReviewRequestedEvent(
                workflow.Id, email.Id,
                review.Id,
                review.ReviewType,
                review.Priority,
                orchResult.Reasoning,
                $"Orchestrator routed to human review. Category: {classResult.Category} ({classResult.Confidence:P0} confidence).",
                DateTimeOffset.UtcNow), ct);

            _logger.LogInformation(
                "Orchestrator routed directly to Human Review — created HumanReview {ReviewId}",
                review.Id);
        }

        if (orchResult.NextAgent == NextAgentName.InvoiceAgent)
        {
            try
            {
                var (analysisId, invoiceResult) = await RunInvoiceAgentAsync(
                    workflow, email, attachmentContexts, ct);
                invoiceAnalysisId = analysisId;

                // Refine knowledge with specialized agent result
                knowledge.RefinedCategory   = classResult.Category; // Invoice doesn't change category
                knowledge.RefinedConfidence = invoiceResult.Confidence;
                knowledge.RefinedReasoning  = invoiceResult.Summary;
                knowledge.CurrentCategory   = classResult.Category;
                knowledge.CurrentConfidence = invoiceResult.Confidence;
                knowledge.CurrentReasoning  = invoiceResult.Summary;
                knowledge.UpdatedAt         = DateTimeOffset.UtcNow;
                await _knowledgeRepo.UpdateAsync(knowledge, ct);

                // Detect if specialized agent couldn't find required fields (low confidence)
                categoryConflict = _conflictDetector.DetectLowConfidence(
                    workflow.Id, email.Id,
                    AgentTypes.Invoice, classResult.Category,
                    invoiceResult.Confidence, _settings);
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
                var (analysisId, contractResult) = await RunContractAgentAsync(
                    workflow, email, attachmentContexts, ct);
                contractAnalysisId = analysisId;

                // ContractType from the agent could differ from the Classification category.
                var specializedCategory = contractResult.ContractType ?? classResult.Category;

                knowledge.RefinedCategory   = specializedCategory;
                knowledge.RefinedConfidence = contractResult.Confidence;
                knowledge.RefinedReasoning  = contractResult.Reasoning;
                knowledge.CurrentCategory   = specializedCategory;
                knowledge.CurrentConfidence = contractResult.Confidence;
                knowledge.CurrentReasoning  = contractResult.Reasoning;
                knowledge.UpdatedAt         = DateTimeOffset.UtcNow;
                await _knowledgeRepo.UpdateAsync(knowledge, ct);

                // Check for category mismatch (Classification said "Contract" but agent
                // identified a different contract type with higher confidence).
                categoryConflict =
                    _conflictDetector.DetectCategoryMismatch(
                        workflow.Id, email.Id,
                        classResult, AgentTypes.Contract,
                        specializedCategory, contractResult.Confidence)
                    ?? _conflictDetector.DetectLowConfidence(
                        workflow.Id, email.Id,
                        AgentTypes.Contract, specializedCategory,
                        contractResult.Confidence, _settings);
            }
            catch (Exception ex)
            {
                await FailWorkflowAsync(workflow, email, AgentTypes.Contract, null, ex, ct);
                return;
            }
        }

        if (categoryConflict is not null)
            await SaveAndBroadcastConflictAsync(categoryConflict, email, ct);

        // ═══ PHASE 4: ESCALATION (when any conflict exists) ═══════════════════
        var hasConflict = lowConfidenceConflict is not null || categoryConflict is not null;

        if (hasConflict)
        {
            workflow.Status = WorkflowStatus.Escalated;
            await _workflowRepo.SaveAsync(workflow, ct);
            await _broadcaster.BroadcastWorkflowUpdatedAsync(new WorkflowUpdatedEvent(
                workflow.Id, email.Id, workflow.Status,
                WorkflowStepName.AnalyzingTaxonomy, null,
                DateTimeOffset.UtcNow), ct);

            var primaryConflict = categoryConflict ?? lowConfidenceConflict!;
            var escalationReason = primaryConflict.Description;

            // Phase 4a: Taxonomy Evolution
            TaxonomyProposal? proposal = null;
            if (_settings.EnableTaxonomyEvolution)
            {
                proposal = await RunTaxonomyEvolutionAsync(
                    workflow, email, classResult, knowledge, escalationReason, ct);
            }

            // Phase 4b: Human Collaboration
            if (_settings.EnableHumanCollaboration)
            {
                await RunHumanCollaborationAsync(
                    workflow, email, classResult, knowledge,
                    escalationReason, proposal, primaryConflict, ct);
            }
        }

        // ═══ PHASE 5: FINALIZE ════════════════════════════════════════════════
        await FinalizeAsync(
            workflow, email, classResult, orchResult,
            invoiceAnalysisId, contractAnalysisId, ct);
    }

    // ── Phase implementations ─────────────────────────────────────────────────

    private async Task<ClassificationResult> RunClassificationAsync(
        Workflow workflow, Email email, CancellationToken ct)
    {
        var now   = DateTimeOffset.UtcNow;
        var execId = UlidGenerator.NewUlid();

        var execution = CreateExecution(execId, workflow.Id, email.Id,
            AgentTypes.Classification, now,
            JsonSerializer.Serialize(new
            {
                subject     = email.Subject,
                bodyPreview = email.BodyPlainText.Length > 200
                    ? email.BodyPlainText[..200]
                    : email.BodyPlainText,
            }));

        await _executionRepo.SaveAsync(execution, ct);
        await _broadcaster.BroadcastStartedAsync(new AgentStartedEvent(
            workflow.Id, AgentTypes.Classification, email.Id, DateTimeOffset.UtcNow), ct);

        var start = DateTimeOffset.UtcNow;
        ClassificationResult result;
        try
        {
            result = await _classificationAgent.ClassifyAsync(
                email.Subject, email.BodyPlainText, ct);
        }
        catch (Exception ex)
        {
            execution.Status       = AgentExecutionStatus.Failed;
            execution.ErrorMessage = ex.Message;
            execution.CompletedAt  = DateTimeOffset.UtcNow;
            await _executionRepo.SaveAsync(execution, ct);
            throw;
        }
        var durationMs = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;

        await _classificationRepo.SaveAsync(new Classification
        {
            Id               = UlidGenerator.NewUlid(),
            EmailId          = email.Id,
            AgentExecutionId = execId,
            CategoryType     = result.Category,
            Confidence       = result.Confidence,
            Reasoning        = result.Reasoning,
            Source           = "AGENT",
            CreatedAt        = DateTimeOffset.UtcNow,
        }, ct);

        execution.Status          = AgentExecutionStatus.Completed;
        execution.ConfidenceScore = result.Confidence;
        execution.ReasoningText   = result.Reasoning;
        execution.DurationMs      = durationMs;
        execution.CompletedAt     = DateTimeOffset.UtcNow;
        execution.OutputPayloadJson = JsonSerializer.Serialize(new
        {
            category   = result.Category,
            confidence = result.Confidence,
            reasoning  = result.Reasoning,
        });
        await _executionRepo.SaveAsync(execution, ct);

        await _broadcaster.BroadcastCompletedAsync(new AgentCompletedEvent(
            workflow.Id, AgentTypes.Classification, email.Id,
            result.Category, result.Confidence, result.Reasoning,
            DateTimeOffset.UtcNow), ct);

        _logger.LogInformation(
            "ClassificationAgent completed — category={Category}, confidence={Confidence:P0}",
            result.Category, result.Confidence);

        return result;
    }

    private async Task<OrchestratorResult> RunOrchestrationAsync(
        Workflow workflow, Email email,
        ClassificationResult classResult, CancellationToken ct)
    {
        var now    = DateTimeOffset.UtcNow;
        var execId = UlidGenerator.NewUlid();

        var execution = CreateExecution(execId, workflow.Id, email.Id,
            AgentTypes.Orchestrator, now,
            JsonSerializer.Serialize(new
            {
                category   = classResult.Category,
                confidence = classResult.Confidence,
            }));

        await _executionRepo.SaveAsync(execution, ct);
        await _broadcaster.BroadcastStartedAsync(new AgentStartedEvent(
            workflow.Id, AgentTypes.Orchestrator, email.Id, DateTimeOffset.UtcNow), ct);

        var start = DateTimeOffset.UtcNow;
        OrchestratorResult result;
        try
        {
            result = await _orchestratorAgent.DecideAsync(new OrchestratorRequest(
                workflow.Id, email.Id,
                classResult.Category, classResult.Confidence, classResult.Reasoning), ct);
        }
        catch (Exception ex)
        {
            execution.Status       = AgentExecutionStatus.Failed;
            execution.ErrorMessage = ex.Message;
            execution.CompletedAt  = DateTimeOffset.UtcNow;
            await _executionRepo.SaveAsync(execution, ct);
            throw;
        }
        var durationMs = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;

        await _orchestrationRepo.SaveAsync(new OrchestrationDecision
        {
            Id                     = UlidGenerator.NewUlid(),
            WorkflowId             = workflow.Id,
            AgentExecutionId       = execId,
            ClassificationCategory = classResult.Category,
            NextAgent              = result.NextAgent,
            WorkflowStatus         = result.WorkflowStatus,
            Reasoning              = result.Reasoning,
            DecidedAt              = DateTimeOffset.UtcNow,
            CreatedAt              = DateTimeOffset.UtcNow,
        }, ct);

        execution.Status          = AgentExecutionStatus.Completed;
        execution.DurationMs      = durationMs;
        execution.CompletedAt     = DateTimeOffset.UtcNow;
        execution.OutputPayloadJson = JsonSerializer.Serialize(new
        {
            nextAgent     = result.NextAgent,
            workflowStatus = result.WorkflowStatus,
            reasoning     = result.Reasoning,
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

    private async Task<(string analysisId, InvoiceAnalysisResult result)> RunInvoiceAgentAsync(
        Workflow workflow, Email email,
        IReadOnlyList<AttachmentContext> attachments, CancellationToken ct)
    {
        var now    = DateTimeOffset.UtcNow;
        var execId = UlidGenerator.NewUlid();

        var execution = CreateExecution(execId, workflow.Id, email.Id,
            AgentTypes.Invoice, now, null);

        await _executionRepo.SaveAsync(execution, ct);
        await _broadcaster.BroadcastStartedAsync(new AgentStartedEvent(
            workflow.Id, AgentTypes.Invoice, email.Id, DateTimeOffset.UtcNow), ct);

        var start = DateTimeOffset.UtcNow;
        InvoiceAnalysisResult result;
        try
        {
            result = await _invoiceAgent.ExtractAsync(new InvoiceExtractionRequest(
                workflow.Id, email.Id, email.Subject, email.BodyPlainText, attachments), ct);
        }
        catch (Exception ex)
        {
            execution.Status       = AgentExecutionStatus.Failed;
            execution.ErrorMessage = ex.Message;
            execution.CompletedAt  = DateTimeOffset.UtcNow;
            await _executionRepo.SaveAsync(execution, ct);
            throw;
        }
        var durationMs = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;

        var analysisId = UlidGenerator.NewUlid();
        await _invoiceAnalysisRepo.SaveAsync(new InvoiceAnalysis
        {
            Id               = analysisId,
            EmailId          = email.Id,
            WorkflowId       = workflow.Id,
            AgentExecutionId = execId,
            Supplier         = result.Supplier,
            InvoiceNumber    = result.InvoiceNumber,
            InvoiceDate      = result.InvoiceDate,
            DueDate          = result.DueDate,
            Currency         = result.Currency,
            TotalAmount      = result.TotalAmount,
            Summary          = result.Summary,
            Confidence       = result.Confidence,
            RawOutputJson    = result.RawOutputJson,
            CreatedAt        = DateTimeOffset.UtcNow,
        }, ct);

        execution.Status          = AgentExecutionStatus.Completed;
        execution.ConfidenceScore = result.Confidence;
        execution.DurationMs      = durationMs;
        execution.CompletedAt     = DateTimeOffset.UtcNow;
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

        return (analysisId, result);
    }

    private async Task<(string analysisId, ContractAnalysisResult result)> RunContractAgentAsync(
        Workflow workflow, Email email,
        IReadOnlyList<AttachmentContext> attachments, CancellationToken ct)
    {
        var now    = DateTimeOffset.UtcNow;
        var execId = UlidGenerator.NewUlid();

        var execution = CreateExecution(execId, workflow.Id, email.Id,
            AgentTypes.Contract, now, null);

        await _executionRepo.SaveAsync(execution, ct);
        await _broadcaster.BroadcastStartedAsync(new AgentStartedEvent(
            workflow.Id, AgentTypes.Contract, email.Id, DateTimeOffset.UtcNow), ct);

        var start = DateTimeOffset.UtcNow;
        ContractAnalysisResult result;
        try
        {
            result = await _contractAgent.ExtractAsync(new ContractExtractionRequest(
                workflow.Id, email.Id, email.Subject, email.BodyPlainText, attachments), ct);
        }
        catch (Exception ex)
        {
            execution.Status       = AgentExecutionStatus.Failed;
            execution.ErrorMessage = ex.Message;
            execution.CompletedAt  = DateTimeOffset.UtcNow;
            await _executionRepo.SaveAsync(execution, ct);
            throw;
        }
        var durationMs = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;

        var analysisId = UlidGenerator.NewUlid();
        await _contractAnalysisRepo.SaveAsync(new ContractAnalysis
        {
            Id                  = analysisId,
            EmailId             = email.Id,
            WorkflowId          = workflow.Id,
            AgentExecutionId    = execId,
            ContractType        = result.ContractType,
            PartiesJson         = JsonSerializer.Serialize(result.Parties),
            EffectiveDate       = result.EffectiveDate,
            ExpirationDate      = result.ExpirationDate,
            RenewalClause       = result.RenewalClause,
            KeyObligationsJson  = JsonSerializer.Serialize(result.KeyObligations),
            Confidence          = result.Confidence,
            Reasoning           = result.Reasoning,
            RawOutputJson       = result.RawOutputJson,
            CreatedAt           = DateTimeOffset.UtcNow,
        }, ct);

        execution.Status          = AgentExecutionStatus.Completed;
        execution.ConfidenceScore = result.Confidence;
        execution.DurationMs      = durationMs;
        execution.CompletedAt     = DateTimeOffset.UtcNow;
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

        return (analysisId, result);
    }

    // ── Sprint 3: Escalation phases ───────────────────────────────────────────

    private async Task<TaxonomyProposal?> RunTaxonomyEvolutionAsync(
        Workflow workflow, Email email,
        ClassificationResult classResult,
        WorkflowKnowledge knowledge,
        string escalationReason,
        CancellationToken ct)
    {
        var now    = DateTimeOffset.UtcNow;
        var execId = UlidGenerator.NewUlid();

        var execution = CreateExecution(execId, workflow.Id, email.Id,
            AgentTypes.TaxonomyEvolution, now, null);
        await _executionRepo.SaveAsync(execution, ct);

        await _broadcaster.BroadcastStartedAsync(new AgentStartedEvent(
            workflow.Id, AgentTypes.TaxonomyEvolution, email.Id, DateTimeOffset.UtcNow), ct);

        // Build existing category list from the taxonomy
        var existingCategories = (await _taxonomyRepo.GetAllActiveAsync(ct))
            .Select(c => c.Label)
            .ToList();

        if (existingCategories.Count == 0)
            existingCategories = [.. EmailCategory.All];

        TaxonomyEvolutionResult taxonomyResult;
        var start = DateTimeOffset.UtcNow;
        try
        {
            taxonomyResult = await _taxonomyEvolutionAgent.AnalyzeAsync(
                new TaxonomyEvolutionRequest(
                    workflow.Id, email.Id,
                    email.Subject, email.BodyPlainText,
                    knowledge.CurrentCategory,
                    knowledge.CurrentConfidence,
                    escalationReason,
                    existingCategories), ct);
        }
        catch (Exception ex)
        {
            execution.Status       = AgentExecutionStatus.Failed;
            execution.ErrorMessage = ex.Message;
            execution.CompletedAt  = DateTimeOffset.UtcNow;
            await _executionRepo.SaveAsync(execution, ct);

            _logger.LogWarning(ex,
                "TaxonomyEvolutionAgent failed for workflow {WorkflowId} — continuing without it",
                workflow.Id);
            return null;
        }
        var durationMs = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;

        execution.Status          = AgentExecutionStatus.Completed;
        execution.ConfidenceScore = taxonomyResult.Confidence;
        execution.ReasoningText   = taxonomyResult.Reasoning;
        execution.DurationMs      = durationMs;
        execution.CompletedAt     = DateTimeOffset.UtcNow;
        execution.OutputPayloadJson = taxonomyResult.RawOutputJson;
        await _executionRepo.SaveAsync(execution, ct);

        await _broadcaster.BroadcastCompletedAsync(new AgentCompletedEvent(
            workflow.Id, AgentTypes.TaxonomyEvolution, email.Id,
            taxonomyResult.SuggestedCategory ?? taxonomyResult.BestFitExistingCategory ?? "Unknown",
            taxonomyResult.Confidence,
            taxonomyResult.Reasoning,
            DateTimeOffset.UtcNow), ct);

        // Update WorkflowKnowledge with the suggestion
        var suggestedLabel = taxonomyResult.NewCategorySuggested
            ? taxonomyResult.SuggestedCategory
            : taxonomyResult.BestFitExistingCategory;

        if (suggestedLabel is not null)
        {
            knowledge.SuggestedCategory    = suggestedLabel;
            knowledge.SuggestionConfidence = taxonomyResult.Confidence;
            knowledge.SuggestionReasoning  = taxonomyResult.Reasoning;
            knowledge.UpdatedAt            = DateTimeOffset.UtcNow;
            await _knowledgeRepo.UpdateAsync(knowledge, ct);
        }

        // Only persist a TaxonomyProposal when the agent believes a new category is needed
        if (!taxonomyResult.NewCategorySuggested || taxonomyResult.SuggestedCategory is null)
            return null;

        var proposal = new TaxonomyProposal
        {
            Id             = UlidGenerator.NewUlid(),
            SuggestedLabel = taxonomyResult.SuggestedCategory,
            Status         = "PENDING",
            Confidence     = taxonomyResult.Confidence,
            SampleCount    = 1,
            SampleEmailIdsJson = JsonSerializer.Serialize(new[] { email.Id }),
            SignalsJson        = JsonSerializer.Serialize(new[] { taxonomyResult.Reasoning }),
            SuggestedRouting   = "operations",
            SuggestedExtractionFieldsJson = "[]",
            CreatedByAgent = AgentTypes.TaxonomyEvolution,
            WorkflowId     = workflow.Id,
            EmailId        = email.Id,
            CreatedAt      = DateTimeOffset.UtcNow,
        };
        await _proposalRepo.SaveAsync(proposal, ct);

        await _broadcaster.BroadcastTaxonomySuggestedAsync(new TaxonomySuggestedEvent(
            workflow.Id, email.Id,
            proposal.Id,
            taxonomyResult.SuggestedCategory,
            taxonomyResult.Confidence,
            taxonomyResult.Reasoning,
            DateTimeOffset.UtcNow), ct);

        workflow.Status = WorkflowStatus.AwaitingTaxonomyApproval;
        await _workflowRepo.SaveAsync(workflow, ct);

        _logger.LogInformation(
            "TaxonomyEvolutionAgent proposed new category \"{Category}\" — proposal {ProposalId}",
            taxonomyResult.SuggestedCategory, proposal.Id);

        return proposal;
    }

    private async Task RunHumanCollaborationAsync(
        Workflow workflow, Email email,
        ClassificationResult classResult,
        WorkflowKnowledge knowledge,
        string escalationReason,
        TaxonomyProposal? proposal,
        AgentConflict primaryConflict,
        CancellationToken ct)
    {
        var now    = DateTimeOffset.UtcNow;
        var execId = UlidGenerator.NewUlid();

        var execution = CreateExecution(execId, workflow.Id, email.Id,
            AgentTypes.HumanCollaboration, now, null);
        await _executionRepo.SaveAsync(execution, ct);

        await _broadcaster.BroadcastStartedAsync(new AgentStartedEvent(
            workflow.Id, AgentTypes.HumanCollaboration, email.Id, DateTimeOffset.UtcNow), ct);

        HumanCollaborationResult collabResult;
        var start = DateTimeOffset.UtcNow;
        try
        {
            collabResult = await _humanCollaborationAgent.EvaluateAsync(
                new HumanCollaborationRequest(
                    workflow.Id, email.Id,
                    email.Subject, email.BodyPlainText,
                    knowledge.CurrentCategory,
                    knowledge.CurrentConfidence,
                    escalationReason,
                    knowledge.SuggestedCategory,
                    knowledge.SuggestionConfidence), ct);
        }
        catch (Exception ex)
        {
            execution.Status       = AgentExecutionStatus.Failed;
            execution.ErrorMessage = ex.Message;
            execution.CompletedAt  = DateTimeOffset.UtcNow;
            await _executionRepo.SaveAsync(execution, ct);

            _logger.LogWarning(ex,
                "HumanCollaborationAgent failed for workflow {WorkflowId} — routing to AwaitingReview",
                workflow.Id);

            // Fall back to AwaitingReview without a structured review task
            await SetAwaitingReviewAsync(workflow, email, ct);
            return;
        }
        var durationMs = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;

        execution.Status          = AgentExecutionStatus.Completed;
        execution.DurationMs      = durationMs;
        execution.CompletedAt     = DateTimeOffset.UtcNow;
        execution.OutputPayloadJson = collabResult.RawOutputJson;
        await _executionRepo.SaveAsync(execution, ct);

        await _broadcaster.BroadcastCompletedAsync(new AgentCompletedEvent(
            workflow.Id, AgentTypes.HumanCollaboration, email.Id,
            collabResult.RequiresHumanReview ? "ReviewRequired" : "AutoProceed",
            1f, collabResult.Reasoning,
            DateTimeOffset.UtcNow), ct);

        if (!collabResult.RequiresHumanReview)
        {
            // Agent determined the taxonomy suggestion is sufficient; proceed automatically
            _logger.LogInformation(
                "HumanCollaborationAgent determined no human review needed — workflow {WorkflowId}",
                workflow.Id);
            return;
        }

        // Create a structured HumanReview task
        var review = new HumanReview
        {
            Id              = UlidGenerator.NewUlid(),
            EmailId         = email.Id,
            WorkflowId      = workflow.Id,
            ReviewType      = collabResult.ReviewType,
            Priority        = collabResult.Priority,
            Status          = ReviewStatus.Pending,
            Reason          = escalationReason,
            AgentConfidence = knowledge.CurrentConfidence,
            ConflictId      = primaryConflict.Id,
            QueuedAt        = DateTimeOffset.UtcNow,
            CreatedAt       = DateTimeOffset.UtcNow,
        };
        await _reviewRepo.SaveAsync(review, ct);

        await _broadcaster.BroadcastReviewRequestedAsync(new ReviewRequestedEvent(
            workflow.Id, email.Id,
            review.Id,
            collabResult.ReviewType,
            collabResult.Priority,
            collabResult.Question,
            collabResult.Recommendation,
            DateTimeOffset.UtcNow), ct);

        await SetAwaitingReviewAsync(workflow, email, ct);

        _logger.LogInformation(
            "HumanCollaborationAgent created review {ReviewId} — priority={Priority}",
            review.Id, collabResult.Priority);
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

        // If already set to AwaitingReview/Escalated by the Sprint 3 phases, preserve that status.
        var preserveStatus =
            workflow.Status == WorkflowStatus.AwaitingReview ||
            workflow.Status == WorkflowStatus.AwaitingTaxonomyApproval ||
            workflow.Status == WorkflowStatus.UnderReview;

        var finalWorkflowStatus = preserveStatus
            ? workflow.Status
            : orchResult.NextAgent == NextAgentName.HumanReview
                ? WorkflowStatus.AwaitingReview
                : WorkflowStatus.CompletedAuto;

        var finalEmailStatus = finalWorkflowStatus is
            WorkflowStatus.AwaitingReview or
            WorkflowStatus.AwaitingTaxonomyApproval or
            WorkflowStatus.UnderReview
                ? EmailStatus.AwaitingReview
                : EmailStatus.CompletedAuto;

        var summary = orchResult.NextAgent switch
        {
            NextAgentName.InvoiceAgent   => $"Invoice extracted. Classified as {classResult.Category}.",
            NextAgentName.ContractAgent  => $"Contract analysed. Classified as {classResult.Category}.",
            NextAgentName.HumanReview    => $"Queued for human review. Category: {classResult.Category}.",
            _ => $"Completed without extraction. Category: {classResult.Category}.",
        };

        var resultStatus = orchResult.NextAgent is NextAgentName.InvoiceAgent
            or NextAgentName.ContractAgent
                ? WorkflowResultStatus.CompletedExtracted
                : orchResult.NextAgent == NextAgentName.HumanReview
                    ? WorkflowResultStatus.AwaitingReview
                    : WorkflowResultStatus.Completed;

        await _workflowResultRepo.SaveAsync(new WorkflowResult
        {
            Id                      = UlidGenerator.NewUlid(),
            WorkflowId              = workflow.Id,
            ClassificationCategory  = classResult.Category,
            ClassificationConfidence = classResult.Confidence,
            RoutedToAgent           = orchResult.NextAgent,
            InvoiceAnalysisId       = invoiceAnalysisId,
            ContractAnalysisId      = contractAnalysisId,
            FinalStatus             = resultStatus,
            Summary                 = summary,
            CompletedAt             = now,
            CreatedAt               = now,
        }, ct);

        workflow.Status      = finalWorkflowStatus;
        workflow.CurrentStep = null;
        workflow.OutcomeType = orchResult.NextAgent;
        workflow.CompletedAt = now;
        await _workflowRepo.SaveAsync(workflow, ct);

        email.Status      = finalEmailStatus;
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

    // ── Sprint 3 helpers ──────────────────────────────────────────────────────

    private async Task<WorkflowKnowledge> InitializeKnowledgeAsync(
        Workflow workflow, Email email,
        ClassificationResult classResult, CancellationToken ct)
    {
        var knowledge = new WorkflowKnowledge
        {
            Id                = UlidGenerator.NewUlid(),
            WorkflowId        = workflow.Id,
            EmailId           = email.Id,
            InitialCategory   = classResult.Category,
            InitialConfidence = classResult.Confidence,
            CurrentCategory   = classResult.Category,
            CurrentConfidence = classResult.Confidence,
            CurrentReasoning  = classResult.Reasoning,
            CreatedAt         = DateTimeOffset.UtcNow,
            UpdatedAt         = DateTimeOffset.UtcNow,
        };
        await _knowledgeRepo.SaveAsync(knowledge, ct);
        return knowledge;
    }

    private async Task SaveAndBroadcastConflictAsync(
        AgentConflict conflict, Email email, CancellationToken ct)
    {
        await _conflictRepo.SaveAsync(conflict, ct);

        await _broadcaster.BroadcastConflictDetectedAsync(new ConflictDetectedEvent(
            conflict.WorkflowId, email.Id,
            conflict.Id,
            conflict.ConflictType,
            conflict.SourceAgent,
            conflict.TargetAgent,
            conflict.SourceValue,
            conflict.TargetValue,
            conflict.SourceConfidence,
            conflict.TargetConfidence,
            conflict.Description,
            DateTimeOffset.UtcNow), ct);

        _logger.LogWarning(
            "Conflict detected — type={Type}, source={Source}, target={Target}",
            conflict.ConflictType, conflict.SourceAgent, conflict.TargetAgent);
    }

    private async Task SetAwaitingReviewAsync(
        Workflow workflow, Email email, CancellationToken ct)
    {
        workflow.Status = WorkflowStatus.AwaitingReview;
        await _workflowRepo.SaveAsync(workflow, ct);

        email.Status = EmailStatus.AwaitingReview;
        await _emailRepo.SaveAsync(email, ct);

        await _broadcaster.BroadcastWorkflowUpdatedAsync(new WorkflowUpdatedEvent(
            workflow.Id, email.Id, workflow.Status,
            WorkflowStepName.HumanReview, null,
            DateTimeOffset.UtcNow), ct);
    }

    private async Task FailWorkflowAsync(
        Workflow workflow, Email email,
        string agentType, string? execId,
        Exception ex, CancellationToken ct)
    {
        _logger.LogError(ex,
            "{AgentType} failed — workflow {WorkflowId}", agentType, workflow.Id);

        if (execId is not null)
        {
            var execution = await _executionRepo.GetByIdAsync(execId, ct);
            if (execution is not null)
            {
                execution.Status       = AgentExecutionStatus.Failed;
                execution.ErrorMessage = ex.Message;
                execution.CompletedAt  = DateTimeOffset.UtcNow;
                await _executionRepo.SaveAsync(execution, ct);
            }
        }

        workflow.Status      = WorkflowStatus.Failed;
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

    // ── Shared helpers ────────────────────────────────────────────────────────

    private static AgentExecution CreateExecution(
        string execId, string workflowId, string emailId,
        string agentType, DateTimeOffset now, string? inputJson)
        => new()
        {
            Id               = execId,
            WorkflowId       = workflowId,
            EmailId          = emailId,
            AgentType        = agentType,
            AgentVersion     = "3.0",
            Status           = AgentExecutionStatus.Running,
            InputPayloadJson = inputJson,
            StartedAt        = now,
            CreatedAt        = now,
        };

    private static string MapNextAgentToStepName(string nextAgent) => nextAgent switch
    {
        NextAgentName.InvoiceAgent  => WorkflowStepName.ExtractingInvoice,
        NextAgentName.ContractAgent => WorkflowStepName.ExtractingContract,
        NextAgentName.HumanReview   => WorkflowStepName.HumanReview,
        _                           => WorkflowStepName.Completing,
    };

    private async Task<IReadOnlyList<AttachmentContext>> BuildAttachmentContextsAsync(
        Email email, CancellationToken ct)
    {
        if (!email.Attachments.Any()) return [];

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
