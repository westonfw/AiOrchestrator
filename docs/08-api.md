# 08 - API 设计

## 通用响应

```json
{
  "success": true,
  "data": {},
  "errorCode": null,
  "errorMessage": null
}
```

## Task API

### 创建任务

```http
POST /api/tasks
```

Request:

```json
{
  "scenarioCode": "credit_rating",
  "title": "某某科技信评报告",
  "input": {
    "company_name": "某某科技股份有限公司",
    "period": "2024",
    "report_type": "主体信用分析",
    "materials_text": "..."
  }
}
```

Response:

```json
{
  "id": "...",
  "status": "Created"
}
```

### 启动任务

```http
POST /api/tasks/{taskId}/start
```

### 查询任务列表

```http
GET /api/tasks?scenarioCode=credit_rating&status=Running&pageIndex=1&pageSize=20
```

### 查询任务详情

```http
GET /api/tasks/{taskId}
```

返回：

```json
{
  "task": {},
  "workflowRun": {},
  "steps": [],
  "reviews": [],
  "artifacts": [],
  "evidence": []
}
```

## Review API

### 查询待审核

```http
GET /api/reviews?status=Pending
```

### 审核通过

```http
POST /api/reviews/{reviewId}/approve
```

Request:

```json
{
  "comment": "同意初步结论"
}
```

### 审核驳回

```http
POST /api/reviews/{reviewId}/reject
```

### 审核修改后通过

```http
POST /api/reviews/{reviewId}/modify
```

Request:

```json
{
  "modifiedContent": {},
  "comment": "调整风险等级为中高"
}
```

## Artifact API

```http
GET /api/tasks/{taskId}/artifacts
GET /api/artifacts/{artifactId}
GET /api/artifacts/{artifactId}/download
```

## Evidence API

```http
GET /api/tasks/{taskId}/evidence
POST /api/evidence/{evidenceId}/verify
```

## File API

```http
POST /api/tasks/{taskId}/files
GET /api/files/{fileId}
```

第一版可以先支持 txt、md、json、csv，PDF/Excel 后置。

## Workflow API

```http
GET /api/scenarios
GET /api/scenarios/{scenarioCode}/workflow
GET /api/tasks/{taskId}/trace
```
