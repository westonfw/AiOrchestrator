# 04 - Workflow Engine 设计

## 目标

Workflow Engine 负责加载场景 Workflow 配置，按照依赖关系执行步骤，并保存每一步状态、输入、输出和错误。

第一版只需要支持：

- 串行 / 简单 DAG 执行
- depends_on 依赖
- skill 节点
- agent 节点
- review 节点
- condition 节点可后置
- 失败重试可后置

## Workflow Definition

示例：

```yaml
code: credit_rating_report
name: 信评报告生成流程
version: 0.1.0

steps:
  - id: resolve_company
    name: 公司主体识别
    type: skill
    skill: company_resolution

  - id: financial_analysis
    name: 财务分析
    type: agent
    agent: financial_analyst
    depends_on:
      - resolve_company
```

## Step 类型

| 类型 | 说明 |
|---|---|
| skill | 调用 IAiSkill |
| agent | 调用 AgentExecutor |
| review | 创建 ReviewItem，暂停任务 |
| condition | 条件判断，后置 |
| artifact | 生成产物，后置 |

## 执行器接口

```csharp
public interface IWorkflowExecutor
{
    Task<Guid> StartAsync(Guid taskId, string workflowCode, CancellationToken ct);
    Task ContinueAsync(Guid workflowRunId, CancellationToken ct);
}
```

## 步骤执行接口

```csharp
public interface IWorkflowStepExecutor
{
    string StepType { get; }
    Task<StepExecutionResult> ExecuteAsync(
        WorkflowExecutionContext context,
        WorkflowStepDefinition step,
        CancellationToken ct);
}
```

## 运行上下文

```csharp
public class WorkflowExecutionContext
{
    public Guid TaskId { get; set; }
    public Guid WorkflowRunId { get; set; }
    public JsonObject Input { get; set; } = new();
    public JsonObject Context { get; set; } = new();
    public Dictionary<string, JsonNode?> StepOutputs { get; set; } = new();
}
```

## 核心算法

```text
1. 读取 workflow_run
2. 加载 workflow definition
3. 读取已完成 step_run
4. 找到所有依赖已完成、但自身未完成的 step
5. 按配置顺序执行 ready steps
6. 每个 step 执行前创建 workflow_step_run
7. 执行成功后把 output 合并到 context
8. 执行失败记录错误并标记任务失败
9. 如果遇到 review 节点，任务进入 WaitingReview
10. 所有步骤完成后任务 Succeeded
```

## Context 合并规则

每个步骤输出写入：

```json
{
  "steps": {
    "financial_analysis": {
      "summary": "..."
    }
  }
}
```

同时支持 `output_mapping` 后续映射到顶层，例如：

```yaml
output_mapping:
  company: $.steps.resolve_company.company
```

第一版可以先不实现复杂 JSONPath，直接按 step_id 存储。

## Review 节点

Review 节点执行时：

1. 创建 `review_item`。
2. 标记 step 为 `WaitingReview`。
3. 标记 task 为 `WaitingReview`。
4. 停止 Workflow。
5. 用户审核通过后，API 调用 continue。

## 验收标准

- 可以加载 `scenarios/credit_rating/workflow.yaml`。
- 可以创建 workflow_run。
- 可以执行 skill / agent / review 三类节点。
- step_run 状态准确。
- 任务详情能看到每一步输入输出。
