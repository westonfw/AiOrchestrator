import type {
  AiTask,
  ApiResponse,
  ReviewItem,
  ReviewListData,
  TaskDetail,
  TaskListData,
  TraceListData,
} from './types'

export type CreateTaskPayload = {
  scenarioCode: string
  title: string
  input: Record<string, unknown>
  createdBy?: string
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
    ...init,
  })

  const body = (await response.json()) as ApiResponse<T>
  if (!response.ok || !body.success) {
    throw new Error(body.errorMessage ?? `Request failed: ${response.status}`)
  }

  return body.data
}

export function listTasks(): Promise<TaskListData> {
  return request<TaskListData>('/api/tasks?pageSize=50')
}

export function createTask(payload: CreateTaskPayload): Promise<{ id: string; status: string }> {
  return request<{ id: string; status: string }>('/api/tasks', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function startTask(taskId: string): Promise<{ workflowRunId: string }> {
  return request<{ workflowRunId: string }>(`/api/tasks/${taskId}/start`, {
    method: 'POST',
  })
}

export function getTask(taskId: string): Promise<TaskDetail> {
  return request<TaskDetail>(`/api/tasks/${taskId}`)
}

export function listPendingReviews(): Promise<ReviewListData> {
  return request<ReviewListData>('/api/reviews?status=Pending')
}

export function approveReview(reviewId: string, comment: string): Promise<ReviewItem> {
  return request<ReviewItem>(`/api/reviews/${reviewId}/approve`, {
    method: 'POST',
    body: JSON.stringify({ comment }),
  })
}

export function rejectReview(reviewId: string, comment: string): Promise<ReviewItem> {
  return request<ReviewItem>(`/api/reviews/${reviewId}/reject`, {
    method: 'POST',
    body: JSON.stringify({ comment }),
  })
}

export function modifyReview(
  reviewId: string,
  comment: string,
  modifiedContent: Record<string, unknown>,
): Promise<ReviewItem> {
  return request<ReviewItem>(`/api/reviews/${reviewId}/modify`, {
    method: 'POST',
    body: JSON.stringify({ comment, modifiedContent }),
  })
}

export function verifyEvidence(evidenceId: string): Promise<{ id: string; verified: boolean }> {
  return request<{ id: string; verified: boolean }>(`/api/evidence/${evidenceId}/verify`, {
    method: 'POST',
  })
}

export function getTrace(taskId: string): Promise<TraceListData> {
  return request<TraceListData>(`/api/tasks/${taskId}/trace`)
}

export function getInput(task?: AiTask | null): Record<string, unknown> {
  if (!task?.inputJson) {
    return {}
  }

  try {
    return JSON.parse(task.inputJson) as Record<string, unknown>
  } catch {
    return {}
  }
}
