# 01 - 系统架构

## 总体架构

```text
React Admin Console
        ↓
AiOrchestrator.Api
        ↓
Application Services
        ↓
Workflow Engine
        ↓
┌────────────┬────────────┬────────────┐
│ Agent Exec │ Skill Exec │ Review Exec │
└────────────┴────────────┴────────────┘
        ↓
┌────────────┬────────────┬────────────┐
│ PostgreSQL │ File Store │ LLM Provider│
└────────────┴────────────┴────────────┘
```

## 后端项目划分

```text
src/
├── AiOrchestrator.Api
│   └── Controller、认证、HTTP API
│
├── AiOrchestrator.Domain
│   └── 实体、枚举、领域模型、值对象
│
├── AiOrchestrator.Application
│   └── Service、DTO、接口、用例编排
│
├── AiOrchestrator.Infrastructure
│   └── EF Core、文件存储、LLM Provider、配置加载
│
├── AiOrchestrator.Workflow
│   └── Workflow 定义、执行器、步骤调度
│
├── AiOrchestrator.Agents
│   └── Agent 定义、Prompt 构建、Schema 校验
│
├── AiOrchestrator.Skills
│   └── Skill 接口、Skill Registry、内置 Skill
│
└── AiOrchestrator.Worker
    └── 后台执行任务、队列消费、重试
```

## 前端项目划分

```text
web/
├── src/pages/tasks
├── src/pages/task-detail
├── src/pages/scenarios
├── src/pages/evidence
├── src/pages/review
├── src/pages/artifacts
├── src/services
└── src/components
```

## 执行流程

```text
1. 用户创建 Task
2. API 保存 ai_task
3. API 创建 workflow_run
4. Worker 加载 workflow.yaml
5. Workflow Engine 找到可执行 step
6. 根据 step.type 调用不同 executor
7. Executor 保存 step_run、agent_run、skill_run
8. 输出合并到 workflow context
9. 关键结论保存 Evidence
10. 中间和最终结果保存 Artifact
11. Review 节点暂停等待用户审核
12. 审核通过后继续执行
13. 生成最终报告草稿
```

## 关键设计决策

### 1. Workflow 使用 YAML 配置

原因：便于让不同业务场景通过配置扩展，而不是改代码。

### 2. 第一版执行器可以先串行

第一版不需要复杂并行调度，先支持 depends_on 即可。

### 3. Agent 输出强制 JSON

Agent 节点必须配置 output_schema，模型输出必须校验。

### 4. Skill 可确定性执行

例如财务指标计算、报告渲染、文件保存等不应依赖 LLM。

### 5. Evidence 独立存储

Evidence 不只是文本引用，而是报告可信度和可审计性的核心。

## 部署建议

第一版单机 Docker Compose 即可：

```text
api container
worker container
postgres container
redis container，可选
frontend container
file storage: host volume 或 MinIO
```

## 运行模式

| 模式 | 说明 |
|---|---|
| Mock Mode | 不调用真实 LLM，便于开发和测试 |
| LLM Mode | 调用真实模型 |
| Review Mode | 关键节点暂停人工审核 |
| Auto Mode | 非高风险节点自动执行 |
