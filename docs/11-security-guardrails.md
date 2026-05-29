# 11 - 安全与 Guardrails

## 基本原则

1. LLM 不直接写数据库。
2. LLM 不直接调用外部高风险接口。
3. Agent 输出必须通过 Schema 校验。
4. 敏感 Skill 必须人工审核。
5. 所有执行动作必须留痕。
6. Prompt 和资料中可能包含敏感数据，日志需要脱敏。

## 权限模型

第一版可以简化：

```text
Admin
Analyst
Reviewer
Viewer
```

权限：

| 角色 | 能力 |
|---|---|
| Admin | 管理全部任务、场景配置 |
| Analyst | 创建和执行任务 |
| Reviewer | 审核任务 |
| Viewer | 只读查看 |

## Skill 风险等级

```text
low: 本地计算、文本处理
medium: 外部查询、文件解析
high: 发邮件、提交订单、交易下单、修改数据
```

第一版只实现 low / medium。

## Agent 输出 Guardrail

Agent Prompt 必须包含：

```text
你只能基于输入上下文和证据分析。
不得虚构数据。
不确定时输出 uncertainties。
关键结论必须引用 evidence_id。
输出必须符合 JSON Schema。
```

## 报告 Guardrail

报告生成前检查：

- 是否存在 rating_opinion
- 是否存在 devil_review
- 是否完成 human_review
- 关键结论是否至少有一个 evidence_id

## 审计要求

每个最终 Artifact 应能追溯：

```text
由哪个 task 生成
经过哪些 step
哪些 Agent 输出参与
引用哪些 Evidence
谁审核过
什么时候生成
```
