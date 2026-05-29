# Codex 工作说明

本仓库目标是实现一个「AI 业务任务编排平台」MVP。第一业务场景是「信评报告生成」，但平台底座必须支持未来扩展到交易决策、投研报告、合同审查、运价分析等场景。

## Codex 总原则

1. 先实现可运行骨架，再逐步增强复杂能力。
2. 优先完成 Task + Workflow + Skill + Agent + Evidence + Artifact + Review 的闭环。
3. 不要一开始实现自由 Planner Agent，不要让 LLM 自主决定全流程。
4. Workflow 是主干；Agent 是分析节点；Skill 是确定性能力节点；Evidence 是可信来源；Review 是人工把关。
5. 所有 Agent 输出必须结构化，并通过 JSON Schema 校验。
6. 所有关键结论必须尽量绑定 evidence_id。
7. LLM 不允许直接写业务数据库，不允许直接调用高风险外部接口。
8. 第一版支持上传资料生成信评报告草稿，不要求自动爬取全网数据。

## 推荐技术栈

- Backend: .NET 8 Web API
- Worker: .NET 8 BackgroundService / Hangfire / RabbitMQ worker
- Database: PostgreSQL
- Cache / runtime state: Redis，可后置
- File storage: 本地文件系统，后续可替换 MinIO/S3
- Frontend: React + Ant Design
- LLM Provider: OpenAI-compatible 抽象，先做 Mock + OpenAI-compatible HTTP adapter
- Document extraction: 第一版先做纯文本/Markdown/CSV/JSON 上传；PDF/Excel 可先留接口

## 实现纪律

- 每完成一个阶段，必须保证 `dotnet build` 通过。
- 所有核心领域模型放在 `AiOrchestrator.Domain`。
- 所有接口定义放在 `AiOrchestrator.Application`。
- 所有数据库/文件/LLM 适配放在 `AiOrchestrator.Infrastructure`。
- Workflow、Agent、Skill 执行器要可单元测试。
- 不要把 Prompt、Workflow、Agent 配置硬编码在 C# 中；放在 `scenarios/` 目录。
- 所有 API 返回统一响应结构。

## 第一阶段验收目标

用户可以：

1. 创建一个 `credit_rating_report` 任务。
2. 上传或粘贴公司资料文本。
3. 系统按 `workflow.yaml` 执行步骤。
4. Skill 节点可以执行。
5. Agent 节点可以先用 Mock LLM 输出结构化 JSON。
6. 任务详情可以看到每一步输入、输出、状态、错误。
7. Evidence、Artifact 可以落库。
8. 最终生成一份 Markdown 版信评报告草稿。

## 禁止事项

- 不要直接生成一个无法运行的大而全项目。
- 不要把 Agent 设计成可以自由调用任意 Skill。
- 不要让模型负责计算财务指标；财务指标必须由确定性代码计算。
- 不要在第一版实现真实下单、真实交易、外部敏感动作。
- 不要把密钥写入仓库。
