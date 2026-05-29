# 06 - Evidence、Artifact、Review

## Evidence Store

Evidence 是证据库，负责支撑报告结论。

### EvidenceItem 字段

```json
{
  "id": "ev_001",
  "task_id": "...",
  "source_type": "uploaded_text",
  "source_name": "2024审计报告摘录",
  "page_no": 12,
  "section_title": "资产负债表",
  "quote_text": "截至2024年末，公司流动负债为...",
  "extracted_value": {
    "field": "current_liabilities",
    "value": 1200000000,
    "unit": "CNY"
  },
  "confidence": 0.91,
  "verified": false
}
```

## Evidence 生成原则

1. Skill 可以生成 Evidence。
2. Agent 可以引用 Evidence，但不应随意编造 Evidence。
3. 用户人工审核后，可以把 Evidence 标记为 verified。
4. 最终报告中的关键结论应引用 evidence_id。

## Artifact Store

Artifact 是产物库。

类型包括：

```text
json
markdown
docx
pdf
table
chart
```

第一版至少支持：

- JSON 中间产物
- Markdown 报告草稿

## Review Center

Review 用于关键节点人工审核。

### Review 节点触发

Workflow 中配置：

```yaml
- id: human_review
  name: 人工审核评级意见
  type: review
  depends_on:
    - rating_committee
    - devil_review
```

执行后：

1. 创建 review_item。
2. Task 状态变为 WaitingReview。
3. 前端展示待审核内容。
4. 用户可以通过、驳回、修改。
5. 通过后继续 Workflow。

### Review API

```http
POST /api/reviews/{reviewId}/approve
POST /api/reviews/{reviewId}/reject
POST /api/reviews/{reviewId}/modify
```

### Review 内容

Review 页面应展示：

- 当前步骤输出
- 引用 Evidence
- 反方审查意见
- 模型置信度
- 人工修改区
- 审核按钮

## 最终报告可信要求

报告不应只包含自然语言正文，还应附带：

```text
1. 关键结论
2. 对应 evidence_id
3. 来源文件/资料
4. 人工审核状态
5. 生成时间
6. 生成模型/Agent
```
