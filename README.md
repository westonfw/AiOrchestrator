# AI Business Orchestrator

AI Business Orchestrator 是一个「多 Agent + 多 Skill + 可配置 Workflow」的业务任务编排平台。

第一版以「信评报告生成」作为落地场景，但底层抽象必须支持未来扩展到：

- 交易决策
- 投研报告
- 尽调报告
- 合同审查
- 运价分析
- 报表逻辑分析
- 客服知识问答

## 核心设计

```text
Task
  ↓
Workflow
  ↓
Agent Node / Skill Node / Review Node
  ↓
Evidence Store + Artifact Store + Trace Log
  ↓
Human Review
  ↓
Final Artifact
```

## 关键抽象

| 概念 | 说明 |
|---|---|
| Task | 用户提交的一次业务任务 |
| Workflow | 任务执行流程定义 |
| Agent | 有角色、有 Prompt、有输出 Schema 的分析节点 |
| Skill | 可复用的确定性能力或外部工具调用 |
| Evidence | 证据，支撑结论的来源和摘录 |
| Artifact | 产物，中间 JSON、表格、报告草稿、最终文件 |
| Review | 人工审核节点 |
| Trace | 全链路执行日志 |

## MVP 范围

第一版只做一个稳定闭环：

```text
创建信评任务
  → 上传/输入资料
  → 解析资料
  → 提取基础事实
  → 计算财务指标
  → 财务分析 Agent
  → 风险分析 Agent
  → 评级意见 Agent
  → 反方审查 Agent
  → 人工审核
  → 生成 Markdown 报告草稿
```

## 推荐项目结构

```text
ai-business-orchestrator/
├── AGENTS.md
├── docs/
├── scenarios/
│   └── credit_rating/
│       ├── workflow.yaml
│       ├── agents.yaml
│       ├── prompts/
│       ├── schemas/
│       └── templates/
├── src/
│   ├── AiOrchestrator.Api/
│   ├── AiOrchestrator.Application/
│   ├── AiOrchestrator.Domain/
│   ├── AiOrchestrator.Infrastructure/
│   ├── AiOrchestrator.Workflow/
│   ├── AiOrchestrator.Agents/
│   ├── AiOrchestrator.Skills/
│   ├── AiOrchestrator.Worker/
│   └── AiOrchestrator.Web/
└── tests/
```

## 开工顺序

1. 建 solution 和项目结构。
2. 建核心实体和枚举。
3. 建 PostgreSQL 表结构和 EF Core DbContext。
4. 实现 Workflow YAML 加载。
5. 实现 DAG/串行 Workflow 执行器。
6. 实现 Skill Registry 和几个 Mock Skill。
7. 实现 Agent Registry 和 Mock LLM Provider。
8. 实现任务创建、任务详情、任务执行 API。
9. 实现信评场景。
10. 做 React 管理台。

详细任务见 `docs/13-codex-task-list.md`。

## 当前 MVP 状态

已实现后端第一阶段闭环：

- .NET 8 solution 与分层项目结构。
- Domain 核心实体：Task、WorkflowRun、WorkflowStepRun、AgentRun、SkillRun、EvidenceItem、Artifact、ReviewItem、TraceEvent。
- EF Core DbContext、PostgreSQL 映射与初始 Migration。
- YAML Workflow 加载与串行执行器，支持 `skill`、`agent`、`review`、`depends_on`、步骤状态持久化、失败记录、人工审核暂停和 `context_json` 合并。
- Skill Runtime：`IAiSkill`、`SkillRegistry`、`SkillExecutor`，内置 `company_resolution`、`collect_materials`、`parse_uploaded_file`、`extract_basic_facts`、`calculate_financial_ratios`、`generate_markdown_report`、`generate_credit_report_docx`。
- Agent Runtime：`agents.yaml`、Prompt、Schema 加载，OpenAI-compatible LLM Provider，JSON 输出和轻量 Schema 校验，`agent_run` 落库。测试可显式切换 `MockLlmProvider`。
- API：Task、Review、Artifact、Evidence、Trace、Workflow 查询。
- Web 管理台：任务队列、新建信评任务、启动 Workflow、步骤详情、审核处理、证据标记、报告预览。
- 集成测试覆盖创建信评任务、执行到人工审核、审核通过、生成 Markdown artifact。

## 本地启动

默认未配置 PostgreSQL 时使用 InMemory 数据库，适合快速验收：

```bash
dotnet restore
dotnet build AiOrchestrator.sln
dotnet test AiOrchestrator.sln
dotnet run --project src/AiOrchestrator.Api --urls http://localhost:5073
```

启动 Web 管理台：

```bash
cd src/AiOrchestrator.Web
npm install
npm run dev -- --host 0.0.0.0
```

访问：

```text
http://localhost:5173
```

Web 开发服务器会把 `/api` 代理到 `http://localhost:5073`。

使用 PostgreSQL 时：

```bash
export Database__UseInMemory=false
export ConnectionStrings__Postgres='Host=localhost;Port=5432;Database=ai_orchestrator;Username=postgres;Password=postgres'
dotnet ef database update --project src/AiOrchestrator.Infrastructure --startup-project src/AiOrchestrator.Api
dotnet run --project src/AiOrchestrator.Api --urls http://localhost:5073
```

API 项目也绑定了本仓库专用 User Secrets：

```text
UserSecretsId: 139d1811-05fe-45f2-98ea-f8b45dc746cd
Path: ~/.microsoft/usersecrets/139d1811-05fe-45f2-98ea-f8b45dc746cd/secrets.json
```

可用以下命令调整本地 PostgreSQL 连接：

```bash
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=ai_orchestrator;Username=postgres;Password=postgres" --project src/AiOrchestrator.Api/AiOrchestrator.Api.csproj
dotnet user-secrets set "Database:UseInMemory" "false" --project src/AiOrchestrator.Api/AiOrchestrator.Api.csproj
```

## curl 验收示例

创建任务：

```bash
curl -s -X POST http://localhost:5073/api/tasks \
  -H 'Content-Type: application/json' \
  -d '{
    "scenarioCode": "credit_rating",
    "title": "测试科技信评报告",
    "input": {
      "company_name": "测试科技股份有限公司",
      "period": "2024",
      "report_type": "主体信用分析",
      "materials_text": "测试科技股份有限公司主营企业软件。2024年营业收入 12 亿元，净利润 1.2 亿元，总资产 30 亿元，总负债 16 亿元，流动资产 10 亿元，流动负债 8 亿元。"
    }
  }'
```

启动任务：

```bash
curl -s -X POST http://localhost:5073/api/tasks/{taskId}/start
```

查询任务详情，确认状态为 `WaitingReview`：

```bash
curl -s http://localhost:5073/api/tasks/{taskId}
```

查询待审核项并通过审核：

```bash
curl -s 'http://localhost:5073/api/reviews?status=Pending'

curl -s -X POST http://localhost:5073/api/reviews/{reviewId}/approve \
  -H 'Content-Type: application/json' \
  -d '{"comment":"同意生成草稿"}'
```

审核通过后再次查询任务详情，状态应为 `Succeeded`，并可查看 Markdown 报告 artifact：

```bash
curl -s http://localhost:5073/api/tasks/{taskId}/artifacts
curl -s http://localhost:5073/api/tasks/{taskId}/evidence
curl -s http://localhost:5073/api/tasks/{taskId}/trace
```

## 公共数据 API

第一版提供受控公共数据源接口，用于后续 Skill/Workflow 引用，不允许 Agent 自由联网。已支持上市公司搜索、基础信息和最新股价查询：

```bash
curl -s 'http://localhost:5073/api/public-data/companies/search?keyword=apple&market=us'
curl -s 'http://localhost:5073/api/public-data/companies/AAPL?market=us'
curl -s 'http://localhost:5073/api/public-data/quotes/AAPL?market=us'
```

股价第一版通过公开 Stooq CSV 数据源适配，配置项：

```json
{
  "PublicData": {
    "StooqBaseUrl": "https://stooq.com"
  }
}
```

## LLM 说明

项目默认使用真实 OpenAI-compatible LLM。Agent 只输出符合场景 Schema 的结构化 JSON，不自由调用 Skill，不直接写数据库；财务指标由 `calculate_financial_ratios` 这类确定性 Skill 计算。

本地配置真实 LLM：

```bash
dotnet user-secrets set "Llm:Provider" "OpenAICompatible" --project src/AiOrchestrator.Api/AiOrchestrator.Api.csproj
dotnet user-secrets set "Llm:BaseUrl" "https://api.openai.com/v1" --project src/AiOrchestrator.Api/AiOrchestrator.Api.csproj
dotnet user-secrets set "Llm:ApiKey" "<your-api-key>" --project src/AiOrchestrator.Api/AiOrchestrator.Api.csproj
dotnet user-secrets set "Llm:DefaultModel" "gpt-4.1-mini" --project src/AiOrchestrator.Api/AiOrchestrator.Api.csproj
```

DeepSeek、Qwen 等兼容 `/v1/chat/completions` 的服务只需要替换 `Llm:BaseUrl`、`Llm:ApiKey`、`Llm:DefaultModel`。如果某个兼容服务不支持 `response_format: json_object`，可关闭该开关：

```bash
dotnet user-secrets set "Llm:UseJsonResponseFormat" "false" --project src/AiOrchestrator.Api/AiOrchestrator.Api.csproj
```

真实 LLM 模式仍会保存 `agent_run`，并对输出做 JSON 解析与 Schema 校验；校验失败时对应 workflow step 会失败。

真实 LLM 调用会默认记录 JSON Lines 日志，包含请求 payload、响应 body、HTTP 状态码、耗时和错误信息；`Authorization` 会写成 `<redacted>`，不会记录 API key。默认位置：

```text
logs/llm/llm-calls-YYYYMMDD.jsonl
```

可调整或关闭：

```bash
dotnet user-secrets set "Llm:LogDirectory" "logs/llm" --project src/AiOrchestrator.Api/AiOrchestrator.Api.csproj
dotnet user-secrets set "Llm:LogRequests" "false" --project src/AiOrchestrator.Api/AiOrchestrator.Api.csproj
```

测试项目会显式覆盖 `Llm:Provider=Mock`，不会调用真实模型。
