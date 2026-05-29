# 10 - Worker 与可观测性

## Worker 目标

Worker 负责异步执行 Workflow，避免 API 请求长时间阻塞。

第一版可以有两种方式：

1. API 内直接启动后台 Task，开发快但不够稳。
2. 使用 Hangfire / RabbitMQ，推荐。

## 推荐第一版

如果希望快：

- 先用 .NET BackgroundService + Channel。
- 后续替换为 RabbitMQ 或 Hangfire。

## 任务执行消息

```json
{
  "type": "start_workflow",
  "task_id": "...",
  "workflow_code": "credit_rating_report"
}
```

继续审核后执行：

```json
{
  "type": "continue_workflow",
  "workflow_run_id": "..."
}
```

## TraceEvent

每个关键动作都写 trace_event。

示例：

```json
{
  "event_type": "step_started",
  "message": "开始执行财务分析",
  "payload": {
    "step_id": "financial_analysis",
    "step_type": "agent"
  }
}
```

## 必须记录的事件

```text
task_created
workflow_started
step_started
skill_called
skill_finished
agent_called
agent_finished
review_required
review_approved
artifact_created
evidence_created
step_failed
workflow_finished
```

## 日志要求

- 普通日志用 ILogger。
- 业务执行轨迹写 trace_event。
- LLM 请求输出可记录，但要注意敏感信息脱敏。
- Token 用量写 agent_run.token_usage_json。

## 错误处理

第一版规则：

- Skill 执行失败：step failed，task failed。
- Agent 输出 Schema 校验失败：step failed，task failed。
- Review 等待：task WaitingReview。
- 用户驳回 Review：task Failed 或 WaitingRevision，第一版可 Failed。

## 后续增强

- step retry
- timeout
- cancellation
- parallel execution
- workflow resume
- trace 可视化
