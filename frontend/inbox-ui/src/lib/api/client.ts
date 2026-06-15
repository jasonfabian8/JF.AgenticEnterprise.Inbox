import axios from 'axios'

const api = axios.create({
  baseURL: '/api/v1',
  headers: { 'Content-Type': 'application/json' },
})

// ── Response types ────────────────────────────────────────────────────────────

export interface EmailListItem {
  id: string
  senderEmail: string
  senderName: string
  subject: string
  status: string
  categoryType: string | null
  confidence: number | null
  attachmentCount: number
  receivedAt: string
  processedAt: string | null
  processingDurationMs: number
  hasConflict: boolean
  humanReviewed: boolean
}

export interface EmailListResponse {
  total: number
  page: number
  pageSize: number
  items: EmailListItem[]
}

export interface ClassificationDto {
  categoryType: string
  confidence: number
  reasoning: string
  source: string
  isOverridden: boolean
}

export interface AttachmentDto {
  id: string
  filename: string
  mimeType: string
  sizeBytes: number
  documentType: string | null
  documentTypeConfidence: number | null
}

export interface InvoiceExtractionDto {
  vendorName: string | null
  vendorNameConfidence: number | null
  invoiceNumber: string | null
  invoiceDate: string | null
  dueDate: string | null
  totalAmount: number | null
  taxAmount: number | null
  currency: string | null
  poReference: string | null
  paymentTerms: string | null
  validationStatus: string
  overallConfidence: number
}

export interface RiskFlagDto {
  flagType: string
  severity: string
  excerpt: string
  confidence: number
}

export interface ContractExtractionDto {
  partyA: string | null
  partyB: string | null
  agreementType: string | null
  effectiveDate: string | null
  expiryDate: string | null
  autoRenewal: boolean
  autoRenewalNoticeDays: number | null
  liabilityCapAmount: number | null
  governingLaw: string | null
  overallConfidence: number
  riskFlags: RiskFlagDto[]
}

export interface EmailDetail {
  id: string
  senderEmail: string
  senderName: string
  subject: string
  bodyPlainText: string
  bodyHtml: string
  status: string
  receivedAt: string
  ingestedAt: string
  processedAt: string | null
  processingDurationMs: number
  hasConflict: boolean
  humanReviewed: boolean
  classification: ClassificationDto | null
  attachments: AttachmentDto[]
  invoiceExtraction: InvoiceExtractionDto | null
  contractExtraction: ContractExtractionDto | null
  // Sprint 2 agent-based analysis
  invoiceAnalysis: InvoiceAnalysisDto | null
  contractAnalysis: ContractAnalysisDto | null
}

export interface WorkflowStepDto {
  id: string
  stepOrder: number
  stepName: string
  agentType: string
  status: string
  startedAt: string | null
  completedAt: string | null
  durationMs: number
  inputSummary: string | null
  outputSummary: string | null
}

export interface AgentExecutionDto {
  id: string
  agentType: string
  agentVersion: string
  status: string
  confidenceScore: number | null
  reasoningText: string | null
  durationMs: number
  startedAt: string
  completedAt: string | null
  errorMessage: string | null
  outputPayloadJson: string | null
}

export interface WorkflowStatus {
  workflowId: string
  emailId: string
  status: string
  currentStep: string | null
  outcomeType: string | null
  startedAt: string
  completedAt: string | null
}

export interface AgentExecutionListResponse {
  workflowId: string
  executions: AgentExecutionDto[]
}

// ── Sprint 2 analysis DTOs ────────────────────────────────────────────────────

export interface InvoiceAnalysisDto {
  id: string
  supplier: string | null
  invoiceNumber: string | null
  invoiceDate: string | null
  dueDate: string | null
  currency: string | null
  totalAmount: number | null
  confidence: number
  summary: string
  createdAt: string
}

export interface ContractAnalysisDto {
  id: string
  contractType: string | null
  parties: string[]
  effectiveDate: string | null
  expirationDate: string | null
  renewalClause: string | null
  keyObligations: string[]
  confidence: number
  reasoning: string
  createdAt: string
}

export interface OrchestrationDecisionDto {
  classificationCategory: string
  nextAgent: string
  workflowStatus: string
  reasoning: string
  decidedAt: string
}

export interface WorkflowResultDto {
  finalStatus: string
  classificationCategory: string
  classificationConfidence: number
  routedToAgent: string
  summary: string
  completedAt: string
  invoiceAnalysis: InvoiceAnalysisDto | null
  contractAnalysis: ContractAnalysisDto | null
}

export interface WorkflowDetail {
  workflowId: string
  emailId: string
  status: string
  startedAt: string
  completedAt: string | null
  outcomeType: string | null
  steps: WorkflowStepDto[]
  agentExecutions: AgentExecutionDto[]
  orchestrationDecision: OrchestrationDecisionDto | null
  workflowResult: WorkflowResultDto | null
}

// ── API functions ─────────────────────────────────────────────────────────────

// ── Ingest request / response ─────────────────────────────────────────────────

export interface AttachmentIngestDto {
  filename: string
  mimeType: string
  sizeBytes: number
}

export interface IngestEmailRequest {
  senderEmail: string
  senderName: string
  subject: string
  bodyPlainText: string
  bodyHtml?: string
  receivedAt?: string
  attachments?: AttachmentIngestDto[]
}

export interface IngestEmailResponse {
  emailId: string
  status: string
  ingestedAt: string
}

export const emailApi = {
  list: (page = 1, pageSize = 20, status?: string, categoryType?: string) =>
    api.get<EmailListResponse>('/emails', {
      params: { page, pageSize, status, categoryType },
    }).then(r => r.data),

  get: (id: string) =>
    api.get<EmailDetail>(`/emails/${id}`).then(r => r.data),

  getWorkflow: (emailId: string) =>
    api.get<WorkflowDetail>(`/emails/${emailId}/workflow`).then(r => r.data),

  ingest: (request: IngestEmailRequest) =>
    api.post<IngestEmailResponse>('/emails/ingest', request).then(r => r.data),
}

// ── Sprint 3 DTOs ─────────────────────────────────────────────────────────────

export interface AgentConflictDto {
  id: string
  workflowId: string
  emailId: string
  sourceAgent: string
  targetAgent: string
  conflictType: string
  description: string
  sourceConfidence: number
  targetConfidence: number
  sourceValue: string | null
  targetValue: string | null
  resolution: string | null
  createdAt: string
  resolvedAt: string | null
}

export interface WorkflowKnowledgeDto {
  id: string
  workflowId: string
  initialCategory: string
  initialConfidence: number
  refinedCategory: string | null
  refinedConfidence: number | null
  refinedReasoning: string | null
  suggestedCategory: string | null
  suggestionConfidence: number | null
  suggestionReasoning: string | null
  approvedCategory: string | null
  approvedBy: string | null
  approvedAt: string | null
  currentCategory: string
  currentConfidence: number
  currentReasoning: string
  createdAt: string
  updatedAt: string
}

export interface HumanReviewDto {
  id: string
  emailId: string
  workflowId: string
  reviewType: string
  priority: string
  status: string
  reason: string
  agentConfidence: number
  conflictId: string | null
  assignedTo: string | null
  question: string | null
  recommendation: string | null
  action: string | null
  overrideCategory: string | null
  reviewerNote: string | null
  reviewerId: string | null
  queuedAt: string
  openedAt: string | null
  decidedAt: string | null
}

export interface TaxonomyProposalDto {
  id: string
  suggestedLabel: string
  status: string
  confidence: number
  sampleCount: number
  suggestedRouting: string
  createdByAgent: string
  workflowId: string | null
  emailId: string | null
  decidedBy: string | null
  decidedAt: string | null
  decisionNote: string | null
  createdAt: string
}

export interface ReasoningTimelineEntryDto {
  timestamp: string
  entryType: string
  actor: string
  title: string
  description: string
  confidence: number | null
  status: string | null
  relatedId: string | null
}

export interface WorkflowReasoningTimeline {
  workflowId: string
  entries: ReasoningTimelineEntryDto[]
}

export interface WorkflowDetailExtended {
  workflowId: string
  emailId: string
  status: string
  startedAt: string
  completedAt: string | null
  outcomeType: string | null
  steps: WorkflowStepDto[]
  agentExecutions: AgentExecutionDto[]
  orchestrationDecision: OrchestrationDecisionDto | null
  workflowResult: WorkflowResultDto | null
  conflicts: AgentConflictDto[]
  knowledge: WorkflowKnowledgeDto | null
  humanReviews: HumanReviewDto[]
  taxonomyProposals: TaxonomyProposalDto[]
}

export interface ReviewQueueDto {
  totalPending: number
  urgentCount: number
  reviews: HumanReviewDto[]
}

export interface TaxonomyQueueDto {
  totalPending: number
  proposals: TaxonomyProposalDto[]
}

export interface HumanReviewDecisionRequest {
  action: string
  reviewerId: string
  reviewerNote?: string
  overrideCategory?: string
}

export interface TaxonomyDecisionRequest {
  decision: string
  decidedBy: string
  decisionNote?: string
}

export const workflowApi = {
  getStatus: (workflowId: string) =>
    api.get<WorkflowStatus>(`/workflows/${workflowId}/status`).then(r => r.data),

  getExecutions: (workflowId: string) =>
    api.get<AgentExecutionListResponse>(`/workflows/${workflowId}/executions`).then(r => r.data),

  execute: (workflowId: string) =>
    api.post(`/workflows/${workflowId}/execute`).then(r => r.data),
}

// ── Sprint 3 API ──────────────────────────────────────────────────────────────

export const reasoningApi = {
  getExtended: (emailId: string) =>
    api.get<WorkflowDetailExtended>(`/emails/${emailId}/workflow/extended`).then(r => r.data),

  getTimeline: (emailId: string) =>
    api.get<WorkflowReasoningTimeline>(`/emails/${emailId}/workflow/timeline`).then(r => r.data),

  getKnowledge: (emailId: string) =>
    api.get<WorkflowKnowledgeDto>(`/emails/${emailId}/workflow/knowledge`).then(r => r.data),

  getConflicts: (emailId: string) =>
    api.get<AgentConflictDto[]>(`/emails/${emailId}/workflow/conflicts`).then(r => r.data),
}

export const reviewApi = {
  getPending: () =>
    api.get<ReviewQueueDto>('/reviews').then(r => r.data),

  getById: (id: string) =>
    api.get<HumanReviewDto>(`/reviews/${id}`).then(r => r.data),

  decide: (id: string, request: HumanReviewDecisionRequest) =>
    api.post<HumanReviewDto>(`/reviews/${id}/decide`, request).then(r => r.data),
}

// ── Dashboard stats ───────────────────────────────────────────────────────────

export interface DashboardEmailStats {
  total: number
  completedAuto: number
  completedHuman: number
  awaitingReview: number
  processing: number
  failed: number
  automationRate: number
}

export interface CategoryDistItem {
  category: string
  count: number
}

export interface ConfidenceStats {
  average: number
  high: number
  medium: number
  low: number
}

export interface AgentStat {
  agentType: string
  totalRuns: number
  completed: number
  failed: number
  avgDurationMs: number
  successRate: number
}

export interface ConflictStats {
  total: number
  active: number
  byType: { type: string; count: number }[]
}

export interface ThroughputDay {
  date: string
  ingested: number
  completed: number
}

export interface DashboardStats {
  emails: DashboardEmailStats
  classification: {
    distribution: CategoryDistItem[]
    confidence: ConfidenceStats
  }
  agents: AgentStat[]
  conflicts: ConflictStats
  queues: { pendingReviews: number; pendingTaxonomy: number }
  throughput: ThroughputDay[]
}

export const dashboardApi = {
  getStats: () => api.get<DashboardStats>('/dashboard/stats').then(r => r.data),
}

export const taxonomyApi = {
  getPending: () =>
    api.get<TaxonomyQueueDto>('/taxonomy/proposals').then(r => r.data),

  getById: (id: string) =>
    api.get<TaxonomyProposalDto>(`/taxonomy/proposals/${id}`).then(r => r.data),

  decide: (id: string, request: TaxonomyDecisionRequest) =>
    api.post<TaxonomyProposalDto>(`/taxonomy/proposals/${id}/decide`, request).then(r => r.data),
}
