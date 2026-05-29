# 09 - 前端管理台

## 技术栈

- React
- TypeScript
- Ant Design
- React Router
- Axios / fetch

## 页面清单

### 1. 任务列表页

路径：`/tasks`

功能：

- 按场景筛选
- 按状态筛选
- 查看当前步骤
- 启动任务
- 进入任务详情

字段：

```text
任务标题
场景
状态
当前步骤
创建人
创建时间
更新时间
```

### 2. 创建任务页

路径：`/tasks/new`

第一版只支持信评报告。

表单：

```text
公司名称
期间
报告类型
资料文本
上传文件，可后置
```

### 3. 任务详情页

路径：`/tasks/:id`

这是最重要页面。

Tabs：

```text
概览
Workflow 步骤
Agent 输出
Skill 输出
Evidence
Artifact
Trace
```

Workflow 步骤展示：

```text
步骤名称
类型
状态
开始时间
结束时间
错误信息
输入 JSON
输出 JSON
```

### 4. Review 审核页

路径：`/reviews/:id`

展示：

- 待审核内容
- 引用证据
- 反方意见
- 修改编辑器
- 通过 / 驳回 / 修改后通过

### 5. Artifact 页面

路径：`/tasks/:id/artifacts`

展示：

- Markdown 报告预览
- JSON 中间产物
- 下载按钮

### 6. Evidence 页面

路径：`/tasks/:id/evidence`

展示：

- 来源
- 摘录
- 提取字段
- 置信度
- 是否人工确认

## 第一版 UI 原则

- 不要做复杂拖拽 Workflow。
- 重点做任务详情和调试可观测性。
- JSON 展示可以用简单代码块。
- Markdown 报告用预览组件展示。

## 状态颜色建议

```text
Created: default
Running: processing
WaitingReview: warning
Succeeded: success
Failed: error
Cancelled: default
```
