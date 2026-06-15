using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JF.AgenticEnterprise.Infrastructure.Persistence;

public class InboxDbContext : DbContext
{
    public InboxDbContext(DbContextOptions<InboxDbContext> options) : base(options) { }

    // ── Sprint 1 ──────────────────────────────────────────────────────────────
    public DbSet<Email> Emails => Set<Email>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<AgentExecution> AgentExecutions => Set<AgentExecution>();
    public DbSet<Classification> Classifications => Set<Classification>();
    public DbSet<InvoiceExtraction> InvoiceExtractions => Set<InvoiceExtraction>();
    public DbSet<ContractExtraction> ContractExtractions => Set<ContractExtraction>();
    public DbSet<RiskFlag> RiskFlags => Set<RiskFlag>();
    public DbSet<TaxonomyCategory> TaxonomyCategories => Set<TaxonomyCategory>();
    public DbSet<TaxonomyProposal> TaxonomyProposals => Set<TaxonomyProposal>();
    public DbSet<TaxonomyCandidate> TaxonomyCandidates => Set<TaxonomyCandidate>();
    public DbSet<HumanReview> HumanReviews => Set<HumanReview>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    // ── Sprint 2 ──────────────────────────────────────────────────────────────
    public DbSet<OrchestrationDecision> OrchestrationDecisions => Set<OrchestrationDecision>();
    public DbSet<InvoiceAnalysis> InvoiceAnalyses => Set<InvoiceAnalysis>();
    public DbSet<ContractAnalysis> ContractAnalyses => Set<ContractAnalysis>();
    public DbSet<WorkflowResult> WorkflowResults => Set<WorkflowResult>();

    // ── Sprint 3 ──────────────────────────────────────────────────────────────
    public DbSet<AgentConflict> AgentConflicts => Set<AgentConflict>();
    public DbSet<WorkflowKnowledge> WorkflowKnowledge => Set<WorkflowKnowledge>();

    // Store all DateTimeOffset values as INTEGER (Unix ms) so SQLite can sort/filter them.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToLongConverter>();

        configurationBuilder
            .Properties<DateTimeOffset?>()
            .HaveConversion<NullableDateTimeOffsetToLongConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureEmail(modelBuilder);
        ConfigureWorkflow(modelBuilder);
        ConfigureAgentExecution(modelBuilder);
        ConfigureClassification(modelBuilder);
        ConfigureExtractions(modelBuilder);
        ConfigureTaxonomy(modelBuilder);
        ConfigureHumanReview(modelBuilder);
        ConfigureAuditEntry(modelBuilder);

        // Sprint 2
        ConfigureOrchestrationDecision(modelBuilder);
        ConfigureInvoiceAnalysis(modelBuilder);
        ConfigureContractAnalysis(modelBuilder);
        ConfigureWorkflowResult(modelBuilder);

        // Sprint 3
        ConfigureAgentConflict(modelBuilder);
        ConfigureWorkflowKnowledge(modelBuilder);
        ConfigureHumanReviewSprint3(modelBuilder);
        ConfigureTaxonomyProposalSprint3(modelBuilder);
    }

    // ── Sprint 1 configurations (unchanged) ──────────────────────────────────

    private static void ConfigureEmail(ModelBuilder m)
    {
        m.Entity<Email>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
            e.HasIndex(x => new { x.Status, x.ReceivedAt });
            e.HasIndex(x => x.SenderEmail);

            e.HasMany(x => x.Attachments)
             .WithOne(x => x.Email)
             .HasForeignKey(x => x.EmailId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Workflow)
             .WithOne(x => x.Email)
             .HasForeignKey<Workflow>(x => x.EmailId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Classification)
             .WithOne(x => x.Email)
             .HasForeignKey<Classification>(x => x.EmailId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.InvoiceExtraction)
             .WithOne(x => x.Email)
             .HasForeignKey<InvoiceExtraction>(x => x.EmailId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.ContractExtraction)
             .WithOne(x => x.Email)
             .HasForeignKey<ContractExtraction>(x => x.EmailId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.AgentExecutions)
             .WithOne(x => x.Email)
             .HasForeignKey(x => x.EmailId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.HumanReviews)
             .WithOne(x => x.Email)
             .HasForeignKey(x => x.EmailId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.TaxonomyCandidates)
             .WithOne(x => x.Email)
             .HasForeignKey(x => x.EmailId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.AuditEntries)
             .WithOne(x => x.Email)
             .HasForeignKey(x => x.EmailId)
             .OnDelete(DeleteBehavior.SetNull);

            // Sprint 2: inverse side of InvoiceAnalysis / ContractAnalysis
            e.HasOne(x => x.InvoiceAnalysis)
             .WithOne(x => x.Email)
             .HasForeignKey<InvoiceAnalysis>(x => x.EmailId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.ContractAnalysis)
             .WithOne(x => x.Email)
             .HasForeignKey<ContractAnalysis>(x => x.EmailId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<Attachment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.EmailId);
        });
    }

    private static void ConfigureWorkflow(ModelBuilder m)
    {
        m.Entity<Workflow>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.EmailId).IsUnique();
            e.HasIndex(x => x.Status);

            e.HasMany(x => x.Steps)
             .WithOne(x => x.Workflow)
             .HasForeignKey(x => x.WorkflowId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.AgentExecutions)
             .WithOne(x => x.Workflow)
             .HasForeignKey(x => x.WorkflowId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.HumanReviews)
             .WithOne(x => x.Workflow)
             .HasForeignKey(x => x.WorkflowId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<WorkflowStep>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.WorkflowId, x.StepOrder });
        });
    }

    private static void ConfigureAgentExecution(ModelBuilder m)
    {
        m.Entity<AgentExecution>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EmailId, x.AgentType });
            e.HasIndex(x => new { x.WorkflowId, x.StartedAt });
            e.HasIndex(x => x.StartedAt);
        });
    }

    private static void ConfigureClassification(ModelBuilder m)
    {
        m.Entity<Classification>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.EmailId).IsUnique();
            e.HasIndex(x => new { x.CategoryType, x.CreatedAt });

            e.HasOne(x => x.AgentExecution)
             .WithMany()
             .HasForeignKey(x => x.AgentExecutionId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureExtractions(ModelBuilder m)
    {
        m.Entity<InvoiceExtraction>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.EmailId);

            e.HasOne(x => x.Attachment)
             .WithMany()
             .HasForeignKey(x => x.AttachmentId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AgentExecution)
             .WithMany()
             .HasForeignKey(x => x.AgentExecutionId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<ContractExtraction>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.EmailId);

            e.HasOne(x => x.Attachment)
             .WithMany()
             .HasForeignKey(x => x.AttachmentId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AgentExecution)
             .WithMany()
             .HasForeignKey(x => x.AgentExecutionId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.RiskFlags)
             .WithOne(x => x.ContractExtraction)
             .HasForeignKey(x => x.ContractExtractionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<RiskFlag>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ContractExtractionId);
            e.HasIndex(x => x.Severity);
        });
    }

    private static void ConfigureTaxonomy(ModelBuilder m)
    {
        m.Entity<TaxonomyCategory>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Label).IsUnique();
            e.HasIndex(x => x.Status);
        });

        m.Entity<TaxonomyProposal>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Status);

            e.HasOne(x => x.ResultingCategory)
             .WithMany()
             .HasForeignKey(x => x.ResultingCategoryId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasMany(x => x.Candidates)
             .WithOne(x => x.Proposal)
             .HasForeignKey(x => x.ProposalId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        m.Entity<TaxonomyCandidate>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProposalId);
            e.HasIndex(x => x.CreatedAt);
        });
    }

    private static void ConfigureHumanReview(ModelBuilder m)
    {
        m.Entity<HumanReview>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Status, x.Priority, x.QueuedAt });
            e.HasIndex(x => x.EmailId);
        });
    }

    private static void ConfigureAuditEntry(ModelBuilder m)
    {
        m.Entity<AuditEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EmailId, x.OccurredAt });
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.OccurredAt);
        });
    }

    // ── Sprint 2 configurations ───────────────────────────────────────────────

    private static void ConfigureOrchestrationDecision(ModelBuilder m)
    {
        m.Entity<OrchestrationDecision>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.WorkflowId).IsUnique();

            e.HasOne(x => x.Workflow)
             .WithOne(x => x.OrchestrationDecision)
             .HasForeignKey<OrchestrationDecision>(x => x.WorkflowId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.AgentExecution)
             .WithMany()
             .HasForeignKey(x => x.AgentExecutionId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureInvoiceAnalysis(ModelBuilder m)
    {
        m.Entity<InvoiceAnalysis>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.EmailId).IsUnique();
            e.HasIndex(x => x.WorkflowId).IsUnique();

            // Email relationship is configured on the Email side (ConfigureEmail)

            e.HasOne(x => x.Workflow)
             .WithOne(x => x.InvoiceAnalysis)
             .HasForeignKey<InvoiceAnalysis>(x => x.WorkflowId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AgentExecution)
             .WithMany()
             .HasForeignKey(x => x.AgentExecutionId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureContractAnalysis(ModelBuilder m)
    {
        m.Entity<ContractAnalysis>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.EmailId).IsUnique();
            e.HasIndex(x => x.WorkflowId).IsUnique();

            // Email relationship is configured on the Email side (ConfigureEmail)

            e.HasOne(x => x.Workflow)
             .WithOne(x => x.ContractAnalysis)
             .HasForeignKey<ContractAnalysis>(x => x.WorkflowId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AgentExecution)
             .WithMany()
             .HasForeignKey(x => x.AgentExecutionId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureWorkflowResult(ModelBuilder m)
    {
        m.Entity<WorkflowResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.WorkflowId).IsUnique();

            e.HasOne(x => x.Workflow)
             .WithOne(x => x.WorkflowResult)
             .HasForeignKey<WorkflowResult>(x => x.WorkflowId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.InvoiceAnalysis)
             .WithMany()
             .HasForeignKey(x => x.InvoiceAnalysisId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);

            e.HasOne(x => x.ContractAnalysis)
             .WithMany()
             .HasForeignKey(x => x.ContractAnalysisId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });
    }

    // ── Sprint 3 configurations ───────────────────────────────────────────────

    private static void ConfigureAgentConflict(ModelBuilder m)
    {
        m.Entity<AgentConflict>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.WorkflowId);
            e.HasIndex(x => new { x.WorkflowId, x.ConflictType });

            e.HasOne(x => x.Workflow)
             .WithMany(x => x.AgentConflicts)
             .HasForeignKey(x => x.WorkflowId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Email)
             .WithMany()
             .HasForeignKey(x => x.EmailId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.HumanReviews)
             .WithOne(x => x.Conflict)
             .HasForeignKey(x => x.ConflictId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });
    }

    private static void ConfigureWorkflowKnowledge(ModelBuilder m)
    {
        m.Entity<WorkflowKnowledge>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.WorkflowId).IsUnique();

            e.HasOne(x => x.Workflow)
             .WithOne(x => x.WorkflowKnowledge)
             .HasForeignKey<WorkflowKnowledge>(x => x.WorkflowId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Email)
             .WithMany()
             .HasForeignKey(x => x.EmailId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // Adds Sprint 3 columns to the existing HumanReview table via shadow migration.
    private static void ConfigureHumanReviewSprint3(ModelBuilder m)
    {
        m.Entity<HumanReview>(e =>
        {
            // ConflictId FK is optional — set on the relationship defined in ConfigureAgentConflict.
            // ConflictId column itself is configured here via shadow property if needed;
            // EF Core auto-discovers it from the nav prop, so nothing extra required.
            e.Property(x => x.ConflictId).IsRequired(false);
            e.Property(x => x.OverrideCategory).IsRequired(false);
        });
    }

    // Adds Sprint 3 columns (WorkflowId, EmailId) to the existing TaxonomyProposal table.
    private static void ConfigureTaxonomyProposalSprint3(ModelBuilder m)
    {
        m.Entity<TaxonomyProposal>(e =>
        {
            e.Property(x => x.WorkflowId).IsRequired(false);
            e.Property(x => x.EmailId).IsRequired(false);
            e.HasIndex(x => x.WorkflowId);
        });
    }
}

// ── Value converters for SQLite DateTimeOffset ────────────────────────────────

public sealed class DateTimeOffsetToLongConverter()
    : ValueConverter<DateTimeOffset, long>(
        v => v.ToUnixTimeMilliseconds(),
        v => DateTimeOffset.FromUnixTimeMilliseconds(v));

public sealed class NullableDateTimeOffsetToLongConverter()
    : ValueConverter<DateTimeOffset?, long?>(
        v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
        v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : (DateTimeOffset?)null);
