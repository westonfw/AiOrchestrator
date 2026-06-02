import type {
  AgentTemplate,
  AgentTemplateSummary,
  AiTask,
  ApiResponse,
  ReviewItem,
  ReviewListData,
  SkillInfo,
  TaskDetail,
  TaskListData,
  TraceListData,
  WorkflowTemplate,
  WorkflowTemplateSummary,
} from './types'

export type CreateTaskPayload = {
  scenarioCode: string
  title: string
  input: Record<string, unknown>
  createdBy?: string
}

export type WorkflowStepTemplatePayload = {
  stepId: string
  name: string
  type: string
  skillCode?: string | null
  agentCode?: string | null
  dependsOn: string[]
  sortOrder: number
  dataSourceBindingsJson?: string
}

export type SaveWorkflowTemplatePayload = {
  scenarioCode: string
  name: string
  version: string
  inputSchemaJson: string
  steps: WorkflowStepTemplatePayload[]
  createdBy?: string
}

export type SaveAgentTemplatePayload = {
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
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    headers: { 'Content-Type': 'application/json', ...init?.headers },
    ...init,
  })
  const body = (await response.json()) as ApiResponse<T>
  if (!response.ok || !body.success) {
    throw new Error(body.errorMessage ?? `Request failed: ${response.status}`)
  }
  return body.data
}

// ── Tasks ─────────────────────────────────────────────────────────────────────

export function listTasks(): Promise<TaskListData> {
  return request<TaskListData>('/api/tasks?pageSize=50')
}

export function createTask(payload: CreateTaskPayload): Promise<{ id: string; status: string }> {
  return request<{ id: string; status: string }>('/api/tasks', { method: 'POST', body: JSON.stringify(payload) })
}

export function startTask(taskId: string): Promise<{ taskId: string }> {
  return request<{ taskId: string }>(`/api/tasks/${taskId}/start`, { method: 'POST' })
}

export function getTask(taskId: string): Promise<TaskDetail> {
  return request<TaskDetail>(`/api/tasks/${taskId}`)
}

// ── Reviews ───────────────────────────────────────────────────────────────────

export function listPendingReviews(): Promise<ReviewListData> {
  return request<ReviewListData>('/api/reviews?status=Pending')
}

export function approveReview(reviewId: string, comment: string): Promise<ReviewItem> {
  return request<ReviewItem>(`/api/reviews/${reviewId}/approve`, { method: 'POST', body: JSON.stringify({ comment }) })
}

export function rejectReview(reviewId: string, comment: string): Promise<ReviewItem> {
  return request<ReviewItem>(`/api/reviews/${reviewId}/reject`, { method: 'POST', body: JSON.stringify({ comment }) })
}

export function modifyReview(reviewId: string, comment: string, modifiedContent: Record<string, unknown>): Promise<ReviewItem> {
  return request<ReviewItem>(`/api/reviews/${reviewId}/modify`, { method: 'POST', body: JSON.stringify({ comment, modifiedContent }) })
}

// ── Evidence & Trace ──────────────────────────────────────────────────────────

export function verifyEvidence(evidenceId: string): Promise<{ id: string; verified: boolean }> {
  return request<{ id: string; verified: boolean }>(`/api/evidence/${evidenceId}/verify`, { method: 'POST' })
}

export function getTrace(taskId: string): Promise<TraceListData> {
  return request<TraceListData>(`/api/tasks/${taskId}/trace`)
}

// ── Helpers ───────────────────────────────────────────────────────────────────

export function getInput(task?: AiTask | null): Record<string, unknown> {
  if (!task?.inputJson) return {}
  try { return JSON.parse(task.inputJson) as Record<string, unknown> } catch { return {} }
}

// ── Workflow Templates ────────────────────────────────────────────────────────

export function listWorkflowTemplates(scenarioCode?: string): Promise<{ items: WorkflowTemplateSummary[] }> {
  const qs = scenarioCode ? `?scenarioCode=${encodeURIComponent(scenarioCode)}` : ''
  return request<{ items: WorkflowTemplateSummary[] }>(`/api/workflow-templates${qs}`)
}

export function getWorkflowTemplate(id: string): Promise<WorkflowTemplate> {
  return request<WorkflowTemplate>(`/api/workflow-templates/${id}`)
}

export function createWorkflowTemplate(payload: SaveWorkflowTemplatePayload): Promise<{ id: string }> {
  return request<{ id: string }>('/api/workflow-templates', { method: 'POST', body: JSON.stringify(payload) })
}

export function updateWorkflowTemplate(id: string, payload: SaveWorkflowTemplatePayload): Promise<{ id: string }> {
  return request<{ id: string }>(`/api/workflow-templates/${id}`, { method: 'PUT', body: JSON.stringify(payload) })
}

export function activateWorkflowTemplate(id: string): Promise<{ id: string; isActive: boolean }> {
  return request<{ id: string; isActive: boolean }>(`/api/workflow-templates/${id}/activate`, { method: 'POST' })
}

export function deleteWorkflowTemplate(id: string): Promise<{ id: string }> {
  return request<{ id: string }>(`/api/workflow-templates/${id}`, { method: 'DELETE' })
}

export function cloneWorkflowFromYaml(scenarioCode: string): Promise<{ id: string }> {
  return request<{ id: string }>('/api/workflow-templates/clone-from-yaml', {
    method: 'POST',
    body: JSON.stringify({ scenarioCode }),
  })
}

// ── Agent Templates ───────────────────────────────────────────────────────────

export function listAgentTemplates(scenarioCode?: string): Promise<{ items: AgentTemplateSummary[] }> {
  const qs = scenarioCode ? `?scenarioCode=${encodeURIComponent(scenarioCode)}` : ''
  return request<{ items: AgentTemplateSummary[] }>(`/api/agent-templates${qs}`)
}

export function getAgentTemplate(id: string): Promise<AgentTemplate> {
  return request<AgentTemplate>(`/api/agent-templates/${id}`)
}

export function createAgentTemplate(payload: SaveAgentTemplatePayload): Promise<{ id: string }> {
  return request<{ id: string }>('/api/agent-templates', { method: 'POST', body: JSON.stringify(payload) })
}

export function updateAgentTemplate(id: string, payload: SaveAgentTemplatePayload): Promise<{ id: string }> {
  return request<{ id: string }>(`/api/agent-templates/${id}`, { method: 'PUT', body: JSON.stringify(payload) })
}

export function deleteAgentTemplate(id: string): Promise<{ id: string }> {
  return request<{ id: string }>(`/api/agent-templates/${id}`, { method: 'DELETE' })
}

// ── Skills ────────────────────────────────────────────────────────────────────

export function listSkills(): Promise<{ items: SkillInfo[] }> {
  return request<{ items: SkillInfo[] }>('/api/skills')
}
