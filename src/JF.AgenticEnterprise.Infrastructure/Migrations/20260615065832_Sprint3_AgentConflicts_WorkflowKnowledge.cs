using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JF.AgenticEnterprise.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3_AgentConflicts_WorkflowKnowledge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Emails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    SenderEmail = table.Column<string>(type: "TEXT", nullable: false),
                    SenderName = table.Column<string>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    BodyPlainText = table.Column<string>(type: "TEXT", nullable: false),
                    BodyHtml = table.Column<string>(type: "TEXT", nullable: false),
                    RawStoragePath = table.Column<string>(type: "TEXT", nullable: true),
                    ReceivedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    IngestedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ProcessedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessingDurationMs = table.Column<int>(type: "INTEGER", nullable: false),
                    HasConflict = table.Column<bool>(type: "INTEGER", nullable: false),
                    HumanReviewed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxonomyCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    SignalsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Routing = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedExtractionFieldsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    TotalClassifiedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxonomyCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmailId = table.Column<string>(type: "TEXT", nullable: false),
                    Filename = table.Column<string>(type: "TEXT", nullable: false),
                    MimeType = table.Column<string>(type: "TEXT", nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", nullable: false),
                    ExtractedText = table.Column<string>(type: "TEXT", nullable: true),
                    DocumentType = table.Column<string>(type: "TEXT", nullable: true),
                    DocumentTypeConfidence = table.Column<float>(type: "REAL", nullable: false),
                    OcrStatus = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmailId = table.Column<string>(type: "TEXT", nullable: true),
                    EntityType = table.Column<string>(type: "TEXT", nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", nullable: false),
                    ActorType = table.Column<string>(type: "TEXT", nullable: false),
                    ActorId = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    BeforeValueJson = table.Column<string>(type: "TEXT", nullable: true),
                    AfterValueJson = table.Column<string>(type: "TEXT", nullable: true),
                    Reasoning = table.Column<string>(type: "TEXT", nullable: true),
                    OccurredAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEntries_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmailId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentStep = table.Column<string>(type: "TEXT", nullable: true),
                    ConflictReportJson = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CompletedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CompletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    OutcomeType = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workflows_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxonomyProposals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedLabel = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<string>(type: "TEXT", nullable: true),
                    EmailId = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Confidence = table.Column<float>(type: "REAL", nullable: false),
                    SampleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SampleEmailIdsJson = table.Column<string>(type: "TEXT", nullable: false),
                    SignalsJson = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedRouting = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedExtractionFieldsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedByAgent = table.Column<string>(type: "TEXT", nullable: false),
                    DecidedBy = table.Column<string>(type: "TEXT", nullable: true),
                    DecidedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    DecisionNote = table.Column<string>(type: "TEXT", nullable: true),
                    ResultingCategoryId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxonomyProposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxonomyProposals_TaxonomyCategories_ResultingCategoryId",
                        column: x => x.ResultingCategoryId,
                        principalTable: "TaxonomyCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AgentConflicts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<string>(type: "TEXT", nullable: false),
                    EmailId = table.Column<string>(type: "TEXT", nullable: false),
                    SourceAgent = table.Column<string>(type: "TEXT", nullable: false),
                    TargetAgent = table.Column<string>(type: "TEXT", nullable: false),
                    ConflictType = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    SourceConfidence = table.Column<float>(type: "REAL", nullable: false),
                    TargetConfidence = table.Column<float>(type: "REAL", nullable: false),
                    SourceValue = table.Column<string>(type: "TEXT", nullable: true),
                    TargetValue = table.Column<string>(type: "TEXT", nullable: true),
                    Resolution = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ResolvedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentConflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentConflicts_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentConflicts_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentExecutions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<string>(type: "TEXT", nullable: false),
                    EmailId = table.Column<string>(type: "TEXT", nullable: false),
                    AgentType = table.Column<string>(type: "TEXT", nullable: false),
                    AgentVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    InputPayloadJson = table.Column<string>(type: "TEXT", nullable: true),
                    OutputPayloadJson = table.Column<string>(type: "TEXT", nullable: true),
                    ConfidenceScore = table.Column<float>(type: "REAL", nullable: false),
                    ReasoningText = table.Column<string>(type: "TEXT", nullable: false),
                    FlagsJson = table.Column<string>(type: "TEXT", nullable: false),
                    DurationMs = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CompletedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentExecutions_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentExecutions_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowKnowledge",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<string>(type: "TEXT", nullable: false),
                    EmailId = table.Column<string>(type: "TEXT", nullable: false),
                    InitialCategory = table.Column<string>(type: "TEXT", nullable: false),
                    InitialConfidence = table.Column<float>(type: "REAL", nullable: false),
                    RefinedCategory = table.Column<string>(type: "TEXT", nullable: true),
                    RefinedConfidence = table.Column<float>(type: "REAL", nullable: true),
                    RefinedReasoning = table.Column<string>(type: "TEXT", nullable: true),
                    SuggestedCategory = table.Column<string>(type: "TEXT", nullable: true),
                    SuggestionConfidence = table.Column<float>(type: "REAL", nullable: true),
                    SuggestionReasoning = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovedCategory = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CurrentCategory = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentConfidence = table.Column<float>(type: "REAL", nullable: false),
                    CurrentReasoning = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowKnowledge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowKnowledge_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowKnowledge_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<string>(type: "TEXT", nullable: false),
                    StepOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    StepName = table.Column<string>(type: "TEXT", nullable: false),
                    AgentType = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CompletedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    DurationMs = table.Column<int>(type: "INTEGER", nullable: false),
                    InputSummary = table.Column<string>(type: "TEXT", nullable: true),
                    OutputSummary = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowSteps_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxonomyCandidates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmailId = table.Column<string>(type: "TEXT", nullable: false),
                    ProposalId = table.Column<string>(type: "TEXT", nullable: true),
                    ExtractedSignalsJson = table.Column<string>(type: "TEXT", nullable: false),
                    MatchConfidence = table.Column<float>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxonomyCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxonomyCandidates_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaxonomyCandidates_TaxonomyProposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "TaxonomyProposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HumanReviews",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmailId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<string>(type: "TEXT", nullable: false),
                    ReviewType = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    AgentConfidence = table.Column<float>(type: "REAL", nullable: false),
                    AssignedTo = table.Column<string>(type: "TEXT", nullable: true),
                    QueuedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    OpenedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    DecidedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    Action = table.Column<string>(type: "TEXT", nullable: true),
                    CorrectionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ReviewerNote = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewerId = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewDurationSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ConflictId = table.Column<string>(type: "TEXT", nullable: true),
                    OverrideCategory = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HumanReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HumanReviews_AgentConflicts_ConflictId",
                        column: x => x.ConflictId,
                        principalTable: "AgentConflicts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HumanReviews_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HumanReviews_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Classifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmailId = table.Column<string>(type: "TEXT", nullable: false),
                    AgentExecutionId = table.Column<string>(type: "TEXT", nullable: false),
                    CategoryType = table.Column<string>(type: "TEXT", nullable: false),
                    Confidence = table.Column<float>(type: "REAL", nullable: false),
                    Reasoning = table.Column<string>(type: "TEXT", nullable: false),
                    AlternativeTypesJson = table.Column<string>(type: "TEXT", nullable: false),
                    SignalsDetectedJson = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    IsOverridden = table.Column<bool>(type: "INTEGER", nullable: false),
                    OverriddenBy = table.Column<string>(type: "TEXT", nullable: true),
                    OverriddenAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Classifications_AgentExecutions_AgentExecutionId",
                        column: x => x.AgentExecutionId,
                        principalTable: "AgentExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classifications_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractAnalyses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmailId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<string>(type: "TEXT", nullable: false),
                    AgentExecutionId = table.Column<string>(type: "TEXT", nullable: false),
                    ContractType = table.Column<string>(type: "TEXT", nullable: true),
                    PartiesJson = table.Column<string>(type: "TEXT", nullable: false),
                    EffectiveDate = table.Column<string>(type: "TEXT", nullable: true),
                    ExpirationDate = table.Column<string>(type: "TEXT", nullable: true),
                    RenewalClause = table.Column<string>(type: "TEXT", nullable: true),
                    KeyObligationsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Confidence = table.Column<float>(type: "REAL", nullable: false),
                    Reasoning = table.Column<string>(type: "TEXT", nullable: false),
                    RawOutputJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractAnalyses_AgentExecutions_AgentExecutionId",
                        column: x => x.AgentExecutionId,
                        principalTable: "AgentExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractAnalyses_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractAnalyses_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContractExtractions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmailId = table.Column<string>(type: "TEXT", nullable: false),
                    AttachmentId = table.Column<string>(type: "TEXT", nullable: false),
                    AgentExecutionId = table.Column<string>(type: "TEXT", nullable: false),
                    PartyA = table.Column<string>(type: "TEXT", nullable: true),
                    PartyAConfidence = table.Column<float>(type: "REAL", nullable: false),
                    PartyB = table.Column<string>(type: "TEXT", nullable: true),
                    PartyBConfidence = table.Column<float>(type: "REAL", nullable: false),
                    AgreementType = table.Column<string>(type: "TEXT", nullable: true),
                    AgreementTypeConfidence = table.Column<float>(type: "REAL", nullable: false),
                    EffectiveDate = table.Column<string>(type: "TEXT", nullable: true),
                    EffectiveDateConfidence = table.Column<float>(type: "REAL", nullable: false),
                    ExpiryDate = table.Column<string>(type: "TEXT", nullable: true),
                    ExpiryDateConfidence = table.Column<float>(type: "REAL", nullable: false),
                    AutoRenewal = table.Column<bool>(type: "INTEGER", nullable: true),
                    AutoRenewalNoticeDays = table.Column<int>(type: "INTEGER", nullable: true),
                    LiabilityCapAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    LiabilityCapCurrency = table.Column<string>(type: "TEXT", nullable: true),
                    TerminationForConvenience = table.Column<bool>(type: "INTEGER", nullable: true),
                    GoverningLaw = table.Column<string>(type: "TEXT", nullable: true),
                    PaymentTerms = table.Column<string>(type: "TEXT", nullable: true),
                    OverallConfidence = table.Column<float>(type: "REAL", nullable: false),
                    CalculatedAlertDate = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractExtractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractExtractions_AgentExecutions_AgentExecutionId",
                        column: x => x.AgentExecutionId,
                        principalTable: "AgentExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractExtractions_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractExtractions_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceAnalyses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmailId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<string>(type: "TEXT", nullable: false),
                    AgentExecutionId = table.Column<string>(type: "TEXT", nullable: false),
                    Supplier = table.Column<string>(type: "TEXT", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "TEXT", nullable: true),
                    InvoiceDate = table.Column<string>(type: "TEXT", nullable: true),
                    DueDate = table.Column<string>(type: "TEXT", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<float>(type: "REAL", nullable: false),
                    RawOutputJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceAnalyses_AgentExecutions_AgentExecutionId",
                        column: x => x.AgentExecutionId,
                        principalTable: "AgentExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceAnalyses_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceAnalyses_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceExtractions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmailId = table.Column<string>(type: "TEXT", nullable: false),
                    AttachmentId = table.Column<string>(type: "TEXT", nullable: false),
                    AgentExecutionId = table.Column<string>(type: "TEXT", nullable: false),
                    VendorName = table.Column<string>(type: "TEXT", nullable: true),
                    VendorNameConfidence = table.Column<float>(type: "REAL", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "TEXT", nullable: true),
                    InvoiceNumberConfidence = table.Column<float>(type: "REAL", nullable: false),
                    InvoiceDate = table.Column<string>(type: "TEXT", nullable: true),
                    InvoiceDateConfidence = table.Column<float>(type: "REAL", nullable: false),
                    DueDate = table.Column<string>(type: "TEXT", nullable: true),
                    DueDateConfidence = table.Column<float>(type: "REAL", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    TotalAmountConfidence = table.Column<float>(type: "REAL", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    Subtotal = table.Column<decimal>(type: "TEXT", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", nullable: true),
                    PoReference = table.Column<string>(type: "TEXT", nullable: true),
                    PoReferenceConfidence = table.Column<float>(type: "REAL", nullable: false),
                    PaymentTerms = table.Column<string>(type: "TEXT", nullable: true),
                    LineItemsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ValidationStatus = table.Column<string>(type: "TEXT", nullable: false),
                    ValidationChecksJson = table.Column<string>(type: "TEXT", nullable: false),
                    OverallConfidence = table.Column<float>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceExtractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceExtractions_AgentExecutions_AgentExecutionId",
                        column: x => x.AgentExecutionId,
                        principalTable: "AgentExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceExtractions_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceExtractions_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrchestrationDecisions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<string>(type: "TEXT", nullable: false),
                    AgentExecutionId = table.Column<string>(type: "TEXT", nullable: false),
                    ClassificationCategory = table.Column<string>(type: "TEXT", nullable: false),
                    NextAgent = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowStatus = table.Column<string>(type: "TEXT", nullable: false),
                    Reasoning = table.Column<string>(type: "TEXT", nullable: false),
                    DecidedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrchestrationDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrchestrationDecisions_AgentExecutions_AgentExecutionId",
                        column: x => x.AgentExecutionId,
                        principalTable: "AgentExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrchestrationDecisions_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiskFlags",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ContractExtractionId = table.Column<string>(type: "TEXT", nullable: false),
                    FlagType = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    Excerpt = table.Column<string>(type: "TEXT", nullable: false),
                    PageReference = table.Column<int>(type: "INTEGER", nullable: true),
                    Confidence = table.Column<float>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiskFlags_ContractExtractions_ContractExtractionId",
                        column: x => x.ContractExtractionId,
                        principalTable: "ContractExtractions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowResults",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<string>(type: "TEXT", nullable: false),
                    ClassificationCategory = table.Column<string>(type: "TEXT", nullable: false),
                    ClassificationConfidence = table.Column<float>(type: "REAL", nullable: false),
                    RoutedToAgent = table.Column<string>(type: "TEXT", nullable: false),
                    InvoiceAnalysisId = table.Column<string>(type: "TEXT", nullable: true),
                    ContractAnalysisId = table.Column<string>(type: "TEXT", nullable: true),
                    FinalStatus = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowResults_ContractAnalyses_ContractAnalysisId",
                        column: x => x.ContractAnalysisId,
                        principalTable: "ContractAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkflowResults_InvoiceAnalyses_InvoiceAnalysisId",
                        column: x => x.InvoiceAnalysisId,
                        principalTable: "InvoiceAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkflowResults_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentConflicts_EmailId",
                table: "AgentConflicts",
                column: "EmailId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentConflicts_WorkflowId",
                table: "AgentConflicts",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentConflicts_WorkflowId_ConflictType",
                table: "AgentConflicts",
                columns: new[] { "WorkflowId", "ConflictType" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutions_EmailId_AgentType",
                table: "AgentExecutions",
                columns: new[] { "EmailId", "AgentType" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutions_StartedAt",
                table: "AgentExecutions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutions_WorkflowId_StartedAt",
                table: "AgentExecutions",
                columns: new[] { "WorkflowId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_EmailId",
                table: "Attachments",
                column: "EmailId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_EmailId_OccurredAt",
                table: "AuditEntries",
                columns: new[] { "EmailId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_EntityType_EntityId",
                table: "AuditEntries",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_OccurredAt",
                table: "AuditEntries",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Classifications_AgentExecutionId",
                table: "Classifications",
                column: "AgentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Classifications_CategoryType_CreatedAt",
                table: "Classifications",
                columns: new[] { "CategoryType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Classifications_EmailId",
                table: "Classifications",
                column: "EmailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractAnalyses_AgentExecutionId",
                table: "ContractAnalyses",
                column: "AgentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAnalyses_EmailId",
                table: "ContractAnalyses",
                column: "EmailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractAnalyses_WorkflowId",
                table: "ContractAnalyses",
                column: "WorkflowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractExtractions_AgentExecutionId",
                table: "ContractExtractions",
                column: "AgentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractExtractions_AttachmentId",
                table: "ContractExtractions",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractExtractions_EmailId",
                table: "ContractExtractions",
                column: "EmailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Emails_IdempotencyKey",
                table: "Emails",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Emails_SenderEmail",
                table: "Emails",
                column: "SenderEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_Status_ReceivedAt",
                table: "Emails",
                columns: new[] { "Status", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HumanReviews_ConflictId",
                table: "HumanReviews",
                column: "ConflictId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanReviews_EmailId",
                table: "HumanReviews",
                column: "EmailId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanReviews_Status_Priority_QueuedAt",
                table: "HumanReviews",
                columns: new[] { "Status", "Priority", "QueuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HumanReviews_WorkflowId",
                table: "HumanReviews",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAnalyses_AgentExecutionId",
                table: "InvoiceAnalyses",
                column: "AgentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAnalyses_EmailId",
                table: "InvoiceAnalyses",
                column: "EmailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAnalyses_WorkflowId",
                table: "InvoiceAnalyses",
                column: "WorkflowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceExtractions_AgentExecutionId",
                table: "InvoiceExtractions",
                column: "AgentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceExtractions_AttachmentId",
                table: "InvoiceExtractions",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceExtractions_EmailId",
                table: "InvoiceExtractions",
                column: "EmailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrchestrationDecisions_AgentExecutionId",
                table: "OrchestrationDecisions",
                column: "AgentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrchestrationDecisions_WorkflowId",
                table: "OrchestrationDecisions",
                column: "WorkflowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskFlags_ContractExtractionId",
                table: "RiskFlags",
                column: "ContractExtractionId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskFlags_Severity",
                table: "RiskFlags",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyCandidates_CreatedAt",
                table: "TaxonomyCandidates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyCandidates_EmailId",
                table: "TaxonomyCandidates",
                column: "EmailId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyCandidates_ProposalId",
                table: "TaxonomyCandidates",
                column: "ProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyCategories_Label",
                table: "TaxonomyCategories",
                column: "Label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyCategories_Status",
                table: "TaxonomyCategories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyProposals_ResultingCategoryId",
                table: "TaxonomyProposals",
                column: "ResultingCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyProposals_Status",
                table: "TaxonomyProposals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyProposals_WorkflowId",
                table: "TaxonomyProposals",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowKnowledge_EmailId",
                table: "WorkflowKnowledge",
                column: "EmailId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowKnowledge_WorkflowId",
                table: "WorkflowKnowledge",
                column: "WorkflowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowResults_ContractAnalysisId",
                table: "WorkflowResults",
                column: "ContractAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowResults_InvoiceAnalysisId",
                table: "WorkflowResults",
                column: "InvoiceAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowResults_WorkflowId",
                table: "WorkflowResults",
                column: "WorkflowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_EmailId",
                table: "Workflows",
                column: "EmailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_Status",
                table: "Workflows",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSteps_WorkflowId_StepOrder",
                table: "WorkflowSteps",
                columns: new[] { "WorkflowId", "StepOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "Classifications");

            migrationBuilder.DropTable(
                name: "HumanReviews");

            migrationBuilder.DropTable(
                name: "InvoiceExtractions");

            migrationBuilder.DropTable(
                name: "OrchestrationDecisions");

            migrationBuilder.DropTable(
                name: "RiskFlags");

            migrationBuilder.DropTable(
                name: "TaxonomyCandidates");

            migrationBuilder.DropTable(
                name: "WorkflowKnowledge");

            migrationBuilder.DropTable(
                name: "WorkflowResults");

            migrationBuilder.DropTable(
                name: "WorkflowSteps");

            migrationBuilder.DropTable(
                name: "AgentConflicts");

            migrationBuilder.DropTable(
                name: "ContractExtractions");

            migrationBuilder.DropTable(
                name: "TaxonomyProposals");

            migrationBuilder.DropTable(
                name: "ContractAnalyses");

            migrationBuilder.DropTable(
                name: "InvoiceAnalyses");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "TaxonomyCategories");

            migrationBuilder.DropTable(
                name: "AgentExecutions");

            migrationBuilder.DropTable(
                name: "Workflows");

            migrationBuilder.DropTable(
                name: "Emails");
        }
    }
}
