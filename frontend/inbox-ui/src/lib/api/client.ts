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

export interface WorkflowDetail {
  workflowId: string
  emailId: string
  status: string
  startedAt: string
  completedAt: string | null
  outcomeType: string | null
  steps: WorkflowStepDto[]
  agentExecutions: AgentExecutionDto[]
}

// ── API functions ─────────────────────────────────────────────────────────────

export const emailApi = {
  list: (page = 1, pageSize = 20, status?: string, categoryType?: string) =>
    api.get<EmailListResponse>('/emails', {
      params: { page, pageSize, status, categoryType },
    }).then(r => r.data),

  get: (id: string) =>
    api.get<EmailDetail>(`/emails/${id}`).then(r => r.data),

  getWorkflow: (emailId: string) =>
    api.get<WorkflowDetail>(`/emails/${emailId}/workflow`).then(r => r.data),
}

export const workflowApi = {
  getStatus: (workflowId: string) =>
    api.get<WorkflowStatus>(`/workflows/${workflowId}/status`).then(r => r.data),

  getExecutions: (workflowId: string) =>
    api.get<AgentExecutionListResponse>(`/workflows/${workflowId}/executions`).then(r => r.data),

  execute: (workflowId: string) =>
    api.post(`/workflows/${workflowId}/execute`).then(r => r.data),
}
