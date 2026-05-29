# 13 - Codex 开工任务清单

下面任务可以逐条交给 Codex 执行。每个任务尽量小，完成后要能 build 或通过简单测试。

## Task 001 - 创建解决方案和项目结构

请创建 .NET 8 solution：

```text
AiOrchestrator.sln
src/AiOrchestrator.Api
src/AiOrchestrator.Domain
src/AiOrchestrator.Application
src/AiOrchestrator.Infrastructure
src/AiOrchestrator.Workflow
src/AiOrchestrator.Agents
src/AiOrchestrator.Skills
src/AiOrchestrator.Worker
```

要求：

- Api 引用 Application、Infrastructure。
- Application 引用 Domain。
- Infrastructure 引用 Application、Domain。
- Workflow / Agents / Skills 引用 Application、Domain。
- Worker 引用 Application、Infrastructure、Workflow。
- 添加 `Directory.Build.props`，统一 Nullable、ImplicitUsings。
- 确保 `dotnet build` 通过。

## Task 002 - 实现核心枚举和实体

在 Domain 中实现：

- AiTask
- WorkflowRun
- WorkflowStepRun
- AgentRun
- SkillRun
- EvidenceItem
- Artifact
- ReviewItem
- UploadedFile
- TraceEvent

同时实现枚举：

- TaskStatus
- WorkflowRunStatus
- StepRunStatus
- ReviewStatus
- ArtifactType

要求：

- 使用 Guid 主键。
- CreatedAt 使用 DateTimeOffset。
- JSON 字段第一版用 string 存储。

## Task 003 - 实现 EF Core DbContext 和 Migration

在 Infrastructure 中实现：

- AiOrchestratorDbContext
- EntityTypeConfiguration
- PostgreSQL 支持
- 初始 Migration

要求：

- 表名使用 snake_case。
- 字段使用 snake_case。
- JSON 字段用 jsonb。
- 添加基础索引。

## Task 004 - 实现 Task API

实现：

```http
POST /api/tasks
POST /api/tasks/{taskId}/start
GET /api/tasks
GET /api/tasks/{taskId}
```

要求：

- 创建任务只保存，不立即执行。
- start 创建 workflow_run，并发送执行请求。
- 第一版可以直接调用 WorkflowExecutor。

## Task 005 - 实现 Workflow YAML Loader

实现：

- WorkflowDefinition
- WorkflowStepDefinition
- WorkflowDefinitionLoader

要求：

- 从 `scenarios/{scenario}/workflow.yaml` 加载。
- 支持 fields: code、name、version、steps、depends_on、type、skill、agent。

## Task 006 - 实现 WorkflowExecutor

实现：

- 创建 workflow_run
- 找 ready steps
- 执行 step
- 保存 workflow_step_run
- 合并 context

第一版支持：

- skill step
- agent step
- review step

## Task 007 - 实现 Skill Runtime

实现：

- IAiSkill
- SkillContext
- SkillResult
- SkillRegistry
- SkillExecutor

内置 Skill：

- company_resolution
- collect_materials
- extract_basic_facts
- calculate_financial_ratios
- generate_markdown_report

## Task 008 - 实现 Agent Runtime with Mock LLM

实现：

- AgentDefinition
- AgentDefinitionLoader
- PromptLoader
- ILlmProvider
- MockLlmProvider
- AgentExecutor
- AgentRun 落库

要求：

- MockLlmProvider 根据 agent_code 返回符合 schema 的 JSON。
- 每次执行保存 prompt_text、input_json、output_json。

## Task 009 - 实现 Review 节点

实现：

- ReviewStepExecutor
- Review API
- approve 后继续 Workflow

API：

```http
GET /api/reviews
POST /api/reviews/{reviewId}/approve
POST /api/reviews/{reviewId}/reject
POST /api/reviews/{reviewId}/modify
```

## Task 010 - 实现 Artifact 和 Evidence API

实现：

```http
GET /api/tasks/{taskId}/artifacts
GET /api/artifacts/{artifactId}
GET /api/tasks/{taskId}/evidence
POST /api/evidence/{evidenceId}/verify
```

## Task 011 - 跑通信评 Workflow

使用 `scenarios/credit_rating/workflow.yaml` 跑通：

```text
resolve_company
collect_materials
extract_basic_facts
calculate_financial_ratios
financial_analysis
industry_analysis
risk_analysis
rating_committee
devil_review
human_review
generate_report
```

验收：

- 任务执行到 human_review 暂停。
- 审核通过后生成 Markdown Artifact。

## Task 012 - 实现 React 管理台

创建 React + TypeScript + Ant Design 前端。

页面：

- 任务列表
- 创建任务
- 任务详情
- Review 审核
- Artifact 预览
- Evidence 列表

## Task 013 - 接入 OpenAI-compatible LLM Provider

实现 OpenAI-compatible HTTP 调用。

配置：

```json
{
  "Llm": {
    "Provider": "OpenAICompatible",
    "BaseUrl": "https://api.openai.com/v1",
    "ApiKey": "from env",
    "DefaultModel": "gpt-4.1"
  }
}
```

要求：

- API key 从环境变量读取。
- 支持超时。
- 支持错误日志。
- 不要把密钥写入日志。

## Task 014 - 添加 JSON Schema 校验

实现 Agent 输出校验。

要求：

- 读取 schema 文件。
- 校验失败时 step failed。
- 错误写入 agent_run.error_message。

## Task 015 - 完成 README 运行说明

补充：

- 本地启动步骤
- 数据库初始化
- 创建任务示例
- Mock 模式说明
- LLM 模式说明
