# 12 - 实施路线图

## Phase 0：建仓和骨架

目标：项目能启动、能 build。

任务：

- 创建 .NET solution
- 创建 Api / Domain / Application / Infrastructure / Workflow / Agents / Skills / Worker 项目
- 创建 React 前端项目，可后置
- 配置 PostgreSQL connection
- 配置 appsettings

验收：

```bash
dotnet build
```

通过。

## Phase 1：领域模型和数据库

目标：核心表落地。

任务：

- 实现实体
- 实现 DbContext
- 添加 Migration
- 初始化数据库
- 实现基础 Repository 或 Application Service

验收：

- 可以创建 ai_task。
- 可以查询 task list。

## Phase 2：Workflow Engine

目标：能加载 YAML 并执行 step。

任务：

- WorkflowDefinition 模型
- YAML loader
- Step dependency resolver
- WorkflowExecutor
- workflow_run / step_run 落库

验收：

- 可以执行包含 mock skill 的 workflow。

## Phase 3：Skill Runtime

目标：Skill 可注册、可执行、可记录。

任务：

- IAiSkill
- SkillRegistry
- SkillExecutor
- 内置 Mock Skill
- Evidence / Artifact 生成

验收：

- company_resolution 能执行。
- generate_markdown_report 能生成 artifact。

## Phase 4：Agent Runtime

目标：Agent 可配置、可执行、可校验。

任务：

- AgentDefinition loader
- Prompt loader
- MockLlmProvider
- Json schema validator
- AgentExecutor
- agent_run 落库

验收：

- financial_analyst 能输出结构化 JSON。

## Phase 5：Review Center

目标：Workflow 可暂停和继续。

任务：

- review step executor
- Review API
- approve 后继续 workflow

验收：

- 执行到 human_review 后暂停。
- approve 后生成报告。

## Phase 6：前端管理台

目标：能操作完整流程。

任务：

- 任务列表
- 创建任务
- 任务详情
- Step 输出查看
- Review 页面
- Artifact 页面

验收：

- 用户可通过 UI 创建任务、审核、查看报告。

## Phase 7：真实 LLM 接入

目标：从 Mock 切换到真实模型。

任务：

- OpenAI-compatible provider
- 模型配置
- API key 从环境变量读取
- 超时和错误处理

验收：

- Agent 可调用真实 LLM 生成 JSON。

## Phase 8：增强文档解析

目标：支持更多资料来源。

任务：

- PDF 文本提取
- Excel / CSV 表格解析
- Evidence page_no 支持
- 文件预览

验收：

- 上传 PDF/Excel 后可提取文本和表格。
