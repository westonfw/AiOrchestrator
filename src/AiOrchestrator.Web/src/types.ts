export type ApiResponse<T> = {
  success: boolean
  data: T
  errorCode?: string | null
  errorMessage?: string | null
}

export type TaskStatus =
  | 'Created'
  | 'Running'
  | 'WaitingReview'
  | 'Succeeded'
  | 'Failed'
  | 'Cancelled'

export type StepStatus =
  | 'Pending'
  | 'Running'
  | 'Succeeded'
  | 'Failed'
  | 'WaitingReview'
  | 'Skipped'

export type ReviewStatus = 'Pending' | 'Approved' | 'Rejected' | 'Modified'

export type AiTask = {
  id: string
  scenarioCode: string
  title: string
  inputJson: string
  status: TaskStatus
  currentStep?: string | null
  createdBy?: string | null
  createdAt: string
  updatedAt: string
}

export type WorkflowRun = {
  id: string
  taskId: string
  workflowCode: string
  workflowVersion: string
  status: string
  contextJson: string
  startedAt?: string | null
  finishedAt?: string | null
  createdAt: string
}

export type WorkflowStepRun = {
  id: string
  workflowRunId: string
  taskId: string
  stepId: string
  stepName?: string | null
  stepType: string
  status: StepStatus
  inputJson?: string | null
  outputJson?: string | null
  errorMessage?: string | null
  startedAt?: string | null
  finishedAt?: string | null
  createdAt: string
}

export type ReviewItem = {
  id: string
  taskId: string
  stepRunId: string
  title: string
  contentJson: string
  status: ReviewStatus
  reviewer?: string | null
  reviewComment?: string | null
  reviewedAt?: string | null
  createdAt: string
}

export type Artifact = {
  id: string
  taskId: string
  stepRunId?: string | null
  artifactType: string
  name: string
  contentJson?: string | null
  filePath?: string | null
  version: number
  createdAt: string
}

export type EvidenceItem = {
  id: string
  taskId: string
  sourceType: string
  sourceName?: string | null
  sourceUrl?: string | null
  fileId?: string | null
  pageNo?: number | null
  sectionTitle?: string | null
  quoteText?: string | null
  extractedValueJson?: string | null
  confidence: number
  verified: boolean
  createdAt: string
}

export type TraceEvent = {
  id: string
  taskId?: string | null
  workflowRunId?: string | null
  stepRunId?: string | null
  eventType: string
  message?: string | null
  payloadJson?: string | null
  createdAt: string
}

export type TaskDetail = {
  task: AiTask
  workflowRun?: WorkflowRun | null
  steps: WorkflowStepRun[]
  reviews: ReviewItem[]
  artifacts: Artifact[]
  evidence: EvidenceItem[]
}

export type TaskListData = {
  items: AiTask[]
}

export type ReviewListData = {
  items: ReviewItem[]
}

export type TraceListData = {
  items: TraceEvent[]
}

// ── Template types ────────────────────────────────────────────────────────────

export type WorkflowStepTemplateItem = {
  id: string
  stepId: string
  name: string
  type: 'skill' | 'agent' | 'review'
  skillCode?: string | null
  agentCode?: string | null
  dependsOn: string[]
  sortOrder: number
  dataSourceBindingsJson: string
}

export type WorkflowTemplate = {
  id: string
  scenarioCode: string
  name: string
  version: string
  isActive: boolean
  inputSchemaJson: string
  createdBy?: string | null
  createdAt: string
  updatedAt: string
  steps: WorkflowStepTemplateItem[]
}

export type WorkflowTemplateSummary = {
  id: string
  scenarioCode: string
  name: string
  version: string
  isActive: boolean
  createdAt: string
  updatedAt: string
  stepCount: number
}

export type AgentTemplate = {
  id: string
  scenarioCode: string
  agentCode: string
  name: string
  description: string
  model: string
  temperature: number
  systemPrompt: string
  outputSchemaJson: string
  allowedSkills: string[]
  allowedDataSources: string[]
  maxToolCalls: number
  createdBy?: string | null
  createdAt: string
  updatedAt: string
}

export type AgentTemplateSummary = {
  id: string
  scenarioCode: string
  agentCode: string
  name: string
  description: string
  model: string
  temperature: number
  createdAt: string
  updatedAt: string
}

export type SkillInfo = {
  code: string
  name: string
  description: string
  isSensitive: boolean
  requireReview: boolean
}
