# 05 - Agent 与 Skill Runtime

## Skill Runtime

Skill 是平台可复用能力，适合确定性任务或受控外部调用。

### Skill 接口

```csharp
public interface IAiSkill
{
    string Code { get; }
    string Name { get; }
    string Description { get; }
    JsonNode InputSchema { get; }
    JsonNode OutputSchema { get; }

    Task<SkillResult> ExecuteAsync(SkillContext context, JsonNode input, CancellationToken ct);
}
```

### SkillResult

```csharp
public class SkillResult
{
    public bool Success { get; set; }
    public JsonNode? Output { get; set; }
    public List<EvidenceDraft> EvidenceItems { get; set; } = new();
    public List<ArtifactDraft> Artifacts { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
```

### 第一版内置 Skill

| Skill | 说明 |
|---|---|
| company_resolution | 公司名称规范化，第一版可 Mock |
| collect_uploaded_materials | 读取用户上传/输入的资料 |
| extract_basic_facts | 从资料文本提取基础事实，第一版可规则+Mock |
| calculate_financial_ratios | 计算财务指标 |
| generate_markdown_report | 生成 Markdown 报告 |

## Agent Runtime

Agent 是角色分析节点。

### Agent 定义

来自 `agents.yaml`：

```yaml
- code: financial_analyst
  name: 财务分析师
  model: mock
  temperature: 0.2
  system_prompt_file: prompts/financial_analyst.md
  output_schema: schemas/financial_analysis.schema.json
```

### Agent 执行步骤

```text
1. 加载 AgentDefinition
2. 加载 Prompt 文件
3. 构造 messages
4. 注入 workflow context
5. 调用 LLM Provider
6. 获取 raw output
7. 解析 JSON
8. 校验 output_schema
9. 保存 agent_run
10. 返回结构化 output
```

### LLM Provider 抽象

```csharp
public interface ILlmProvider
{
    Task<LlmResult> GenerateJsonAsync(LlmRequest request, CancellationToken ct);
}
```

```csharp
public class LlmRequest
{
    public string Model { get; set; } = "mock";
    public List<LlmMessage> Messages { get; set; } = new();
    public JsonNode? OutputSchema { get; set; }
    public decimal Temperature { get; set; }
}
```

第一版必须实现：

- MockLlmProvider
- OpenAICompatibleProvider，可后置到第二阶段

## Agent 输出要求

Agent 不要直接输出报告正文，而是输出结构化分析。

错误示例：

```text
某公司经营情况较好，偿债能力不错……
```

正确示例：

```json
{
  "summary": "公司收入规模保持增长，但短期偿债压力需关注。",
  "strengths": [
    {
      "claim": "营业收入保持增长",
      "evidence_ids": ["ev_001"],
      "confidence": 0.86
    }
  ],
  "weaknesses": [],
  "risk_level": "medium"
}
```

## Schema 校验

- 所有 Agent 输出必须校验 JSON Schema。
- 校验失败时，第一版可以直接标记 step failed。
- 第二版再做 LLM repair。

## Skill 与 Agent 边界

| 类型 | 应该放在哪里 |
|---|---|
| 财务指标计算 | Skill |
| 文本总结 | Agent |
| 报告写作 | Skill + Agent 组合 |
| 风险识别 | Agent |
| 文件保存 | Skill |
| 权限校验 | 平台服务 |
| 外部敏感动作 | Skill + Review |
