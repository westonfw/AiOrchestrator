# 03 - 数据库设计

第一版使用 PostgreSQL。所有 JSON 字段使用 `jsonb`。

## 表清单

```text
ai_task
workflow_run
workflow_step_run
agent_run
skill_run
evidence_item
artifact
review_item
uploaded_file
trace_event
```

## DDL

```sql
CREATE TABLE ai_task (
    id UUID PRIMARY KEY,
    scenario_code VARCHAR(100) NOT NULL,
    title VARCHAR(500) NOT NULL,
    input_json JSONB NOT NULL,
    status VARCHAR(50) NOT NULL,
    current_step VARCHAR(100),
    created_by VARCHAR(100),
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL
);

CREATE TABLE workflow_run (
    id UUID PRIMARY KEY,
    task_id UUID NOT NULL REFERENCES ai_task(id),
    workflow_code VARCHAR(100) NOT NULL,
    workflow_version VARCHAR(50),
    status VARCHAR(50) NOT NULL,
    context_json JSONB NOT NULL,
    started_at TIMESTAMP,
    finished_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL
);

CREATE TABLE workflow_step_run (
    id UUID PRIMARY KEY,
    workflow_run_id UUID NOT NULL REFERENCES workflow_run(id),
    task_id UUID NOT NULL REFERENCES ai_task(id),
    step_id VARCHAR(100) NOT NULL,
    step_name VARCHAR(200),
    step_type VARCHAR(50) NOT NULL,
    status VARCHAR(50) NOT NULL,
    input_json JSONB,
    output_json JSONB,
    error_message TEXT,
    started_at TIMESTAMP,
    finished_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL
);

CREATE TABLE agent_run (
    id UUID PRIMARY KEY,
    task_id UUID NOT NULL REFERENCES ai_task(id),
    step_run_id UUID NOT NULL REFERENCES workflow_step_run(id),
    agent_code VARCHAR(100) NOT NULL,
    model VARCHAR(100),
    prompt_text TEXT,
    input_json JSONB,
    raw_output TEXT,
    output_json JSONB,
    schema_valid BOOLEAN,
    token_usage_json JSONB,
    status VARCHAR(50),
    error_message TEXT,
    created_at TIMESTAMP NOT NULL
);

CREATE TABLE skill_run (
    id UUID PRIMARY KEY,
    task_id UUID NOT NULL REFERENCES ai_task(id),
    step_run_id UUID NOT NULL REFERENCES workflow_step_run(id),
    skill_code VARCHAR(100) NOT NULL,
    input_json JSONB,
    output_json JSONB,
    status VARCHAR(50),
    error_message TEXT,
    duration_ms INT,
    created_at TIMESTAMP NOT NULL
);

CREATE TABLE evidence_item (
    id UUID PRIMARY KEY,
    task_id UUID NOT NULL REFERENCES ai_task(id),
    source_type VARCHAR(50) NOT NULL,
    source_name VARCHAR(500),
    source_url TEXT,
    file_id UUID,
    page_no INT,
    section_title VARCHAR(500),
    quote_text TEXT,
    extracted_value_json JSONB,
    confidence NUMERIC(5,4),
    verified BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL
);

CREATE TABLE artifact (
    id UUID PRIMARY KEY,
    task_id UUID NOT NULL REFERENCES ai_task(id),
    step_run_id UUID,
    artifact_type VARCHAR(50) NOT NULL,
    name VARCHAR(500) NOT NULL,
    content_json JSONB,
    file_path TEXT,
    version INT DEFAULT 1,
    created_at TIMESTAMP NOT NULL
);

CREATE TABLE review_item (
    id UUID PRIMARY KEY,
    task_id UUID NOT NULL REFERENCES ai_task(id),
    step_run_id UUID NOT NULL REFERENCES workflow_step_run(id),
    title VARCHAR(500) NOT NULL,
    content_json JSONB NOT NULL,
    status VARCHAR(50) NOT NULL,
    reviewer VARCHAR(100),
    review_comment TEXT,
    reviewed_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL
);

CREATE TABLE uploaded_file (
    id UUID PRIMARY KEY,
    task_id UUID REFERENCES ai_task(id),
    original_file_name VARCHAR(500) NOT NULL,
    stored_file_path TEXT NOT NULL,
    content_type VARCHAR(200),
    file_size BIGINT,
    extracted_text TEXT,
    created_at TIMESTAMP NOT NULL
);

CREATE TABLE trace_event (
    id UUID PRIMARY KEY,
    task_id UUID REFERENCES ai_task(id),
    workflow_run_id UUID REFERENCES workflow_run(id),
    step_run_id UUID REFERENCES workflow_step_run(id),
    event_type VARCHAR(100) NOT NULL,
    message TEXT,
    payload_json JSONB,
    created_at TIMESTAMP NOT NULL
);
```

## 索引建议

```sql
CREATE INDEX idx_ai_task_status ON ai_task(status);
CREATE INDEX idx_ai_task_scenario ON ai_task(scenario_code);
CREATE INDEX idx_workflow_run_task ON workflow_run(task_id);
CREATE INDEX idx_step_run_workflow ON workflow_step_run(workflow_run_id);
CREATE INDEX idx_agent_run_task ON agent_run(task_id);
CREATE INDEX idx_skill_run_task ON skill_run(task_id);
CREATE INDEX idx_evidence_task ON evidence_item(task_id);
CREATE INDEX idx_artifact_task ON artifact(task_id);
CREATE INDEX idx_review_task_status ON review_item(task_id, status);
CREATE INDEX idx_trace_task ON trace_event(task_id);
```

## EF Core 要求

- 使用 Guid 作为主键。
- 所有 CreatedAt 使用 UTC。
- JSON 字段可先用 string 存储，后续优化为 JsonDocument / Npgsql JSON 映射。
- Repository 第一版可以简单，不要过度封装。
