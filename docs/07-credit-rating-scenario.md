# 07 - 信评报告场景

## 场景目标

用户输入公司名称和相关资料，系统生成一份可审核的信评报告草稿。

第一版不追求评级结果权威，而追求：

- 流程稳定
- 结构清晰
- 指标可计算
- 结论可追溯
- 人工可审核

## 输入

```json
{
  "company_name": "某某科技股份有限公司",
  "period": "2024",
  "report_type": "主体信用分析",
  "materials_text": "用户粘贴的资料文本，可选",
  "uploaded_file_ids": []
}
```

## 输出

```json
{
  "company_profile": {},
  "financial_analysis": {},
  "industry_analysis": {},
  "risk_analysis": {},
  "rating_opinion": {},
  "devil_review": {},
  "report_artifact_id": "..."
}
```

## Workflow

```text
resolve_company
  ↓
collect_materials
  ↓
extract_basic_facts
  ↓
calculate_financial_ratios
  ↓
financial_analysis
  ↓
industry_analysis
  ↓
risk_analysis
  ↓
rating_committee
  ↓
devil_review
  ↓
human_review
  ↓
generate_report
```

## Agent 分工

### Financial Analyst

职责：

- 分析收入、利润、现金流、资产负债、偿债能力
- 输出优势、弱点、异常指标、不确定事项
- 所有关键结论引用 evidence_id

### Industry Analyst

职责：

- 分析行业景气度、周期性、竞争格局、政策影响
- 第一版可以基于用户资料和 Mock 行业上下文

### Risk Analyst

职责：

- 识别债务风险、流动性风险、诉讼风险、舆情风险、治理风险

### Rating Committee

职责：

- 汇总财务、行业、风险意见
- 输出初步评级方向和主要理由
- 注意：第一版输出的是“内部建议”，不是正式评级

### Devil Advocate

职责：

- 专门反驳评级结论
- 找出证据不足、逻辑跳跃、潜在风险

## 第一版 Mock 数据策略

为了快速跑通流程：

1. company_resolution 返回公司名称标准化结果。
2. extract_basic_facts 从 materials_text 中规则抽取，抽不到则给出空字段。
3. calculate_financial_ratios 支持用户输入少量财务字段，或使用示例字段。
4. Agent 可以先用 MockLlmProvider 输出固定结构。
5. 真实 LLM 接入放到第二阶段。

## 报告模板

Markdown 报告结构：

```markdown
# {company_name} 信用分析报告草稿

## 一、公司概况

## 二、财务分析

## 三、行业与经营分析

## 四、主要风险因素

## 五、初步评级意见

## 六、反方审查意见

## 七、证据列表
```

## 验收标准

- 输入公司名称能创建任务。
- Workflow 可以执行到 human_review 并暂停。
- 审核通过后生成 Markdown 报告。
- 报告可以在 Artifact 页面查看。
- 每个 Agent 输出都能在任务详情页查看。
