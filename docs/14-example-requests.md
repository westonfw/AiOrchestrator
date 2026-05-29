# 14 - 示例请求

## 创建信评任务

```bash
curl -X POST http://localhost:5000/api/tasks \
  -H "Content-Type: application/json" \
  -d '{
    "scenarioCode": "credit_rating",
    "title": "某某科技信评报告",
    "input": {
      "company_name": "某某科技股份有限公司",
      "period": "2024",
      "report_type": "主体信用分析",
      "materials_text": "公司2024年营业收入10亿元，净利润8000万元，流动资产5亿元，流动负债4亿元，有息债务3亿元。"
    }
  }'
```

## 启动任务

```bash
curl -X POST http://localhost:5000/api/tasks/{taskId}/start
```

## 查询任务详情

```bash
curl http://localhost:5000/api/tasks/{taskId}
```

## 审核通过

```bash
curl -X POST http://localhost:5000/api/reviews/{reviewId}/approve \
  -H "Content-Type: application/json" \
  -d '{"comment":"同意进入报告生成"}'
```
