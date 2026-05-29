# 02 - 领域模型

## 核心实体

```text
AiTask
WorkflowRun
WorkflowStepRun
AgentDefinition
AgentRun
SkillDefinition
SkillRun
EvidenceItem
Artifact
ReviewItem
TraceEvent
UploadedFile
ScenarioDefinition
```

## AiTask

表示一次用户提交的业务任务。

字段：

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | Guid | 任务 ID |
| ScenarioCode | string | 场景编码，例如 credit_rating |
| Title | string | 任务标题 |
| InputJson | jsonb | 用户输入 |
| Status | enum | 任务状态 |
| CurrentStep | string | 当前步骤 |
| CreatedBy | string | 创建人 |
| CreatedAt | DateTime | 创建时间 |
| UpdatedAt | DateTime | 更新时间 |

状态：

```text
Created
Running
WaitingReview
Succeeded
Failed
Cancelled
```

## WorkflowRun

一次任务对应一次 Workflow 执行实例。

字段：

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | Guid | Workflow run ID |
| TaskId | Guid | 任务 ID |
| WorkflowCode | string | Workflow 编码 |
| Version | string | Workflow 版本 |
| Status | enum | 状态 |
| ContextJson | jsonb | 运行上下文 |
| StartedAt | DateTime? | 开始时间 |
| FinishedAt | DateTime? | 结束时间 |

## WorkflowStepRun

Workflow 中每个节点的一次执行记录。

状态：

```text
Pending
Running
Succeeded
Failed
WaitingReview
Skipped
```

步骤类型：

```text
skill
agent
review
condition
artifact
```

## AgentDefinition

Agent 定义来自 `scenarios/{scenario}/agents.yaml`。

字段：

```text
Code
Name
Description
Model
Temperature
SystemPromptFile
AllowedSkills
OutputSchema
MaxRetries
```

## AgentRun

记录一次 Agent 调用。

必须记录：

- Prompt
- 输入上下文
- 原始输出
- 解析后的 JSON 输出
- Schema 校验结果
- 模型名称
- token 用量
- 错误信息

## SkillDefinition

Skill 是平台能力，可由代码注册，也可通过配置暴露。

字段：

```text
Code
Name
Description
InputSchema
OutputSchema
IsSensitive
RequireReview
```

## SkillRun

记录一次 Skill 执行。

必须记录：

- 输入
- 输出
- 状态
- 耗时
- 错误信息
- 产生的 Evidence
- 产生的 Artifact

## EvidenceItem

证据是报告可信度核心。

字段：

```text
Id
TaskId
SourceType
SourceName
SourceUrl
FileId
PageNo
SectionTitle
QuoteText
ExtractedValueJson
Confidence
Verified
CreatedAt
```

## Artifact

产物包括中间产物和最终产物。

类型：

```text
json
markdown
docx
pdf
table
chart
```

## ReviewItem

人工审核项。

字段：

```text
Id
TaskId
StepRunId
Title
ContentJson
Status
Reviewer
ReviewComment
ReviewedAt
```

状态：

```text
Pending
Approved
Rejected
Modified
```

## TraceEvent

记录平台内发生的事件。

类型：

```text
task_created
workflow_started
step_started
step_finished
skill_called
agent_called
review_required
review_approved
artifact_created
evidence_created
error
```
