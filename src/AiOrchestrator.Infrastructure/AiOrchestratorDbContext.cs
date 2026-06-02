using AiOrchestrator.Domain;
using Microsoft.EntityFrameworkCore;

namespace AiOrchestrator.Infrastructure;

public sealed class AiOrchestratorDbContext(DbContextOptions<AiOrchestratorDbContext> options) : DbContext(options)
{
    public DbSet<AiTask> Tasks => Set<AiTask>();
    public DbSet<WorkflowRun> WorkflowRuns => Set<WorkflowRun>();
    public DbSet<WorkflowStepRun> WorkflowStepRuns => Set<WorkflowStepRun>();
    public DbSet<AgentRun> AgentRuns => Set<AgentRun>();
    public DbSet<SkillRun> SkillRuns => Set<SkillRun>();
    public DbSet<EvidenceItem> EvidenceItems => Set<EvidenceItem>();
    public DbSet<Artifact> Artifacts => Set<Artifact>();
    public DbSet<ReviewItem> ReviewItems => Set<ReviewItem>();
    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
    public DbSet<TraceEvent> TraceEvents => Set<TraceEvent>();
    public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();
    public DbSet<WorkflowStepTemplate> WorkflowStepTemplates => Set<WorkflowStepTemplate>();
    public DbSet<AgentTemplate> AgentTemplates => Set<AgentTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiTask>(entity =>
        {
            entity.ToTable("ai_task");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ScenarioCode).HasColumnName("scenario_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
            entity.Property(x => x.InputJson).HasColumnName("input_json").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.CurrentStep).HasColumnName("current_step").HasMaxLength(100);
            entity.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.Status).HasDatabaseName("idx_ai_task_status");
            entity.HasIndex(x => x.ScenarioCode).HasDatabaseName("idx_ai_task_scenario");
        });

        modelBuilder.Entity<WorkflowRun>(entity =>
        {
            entity.ToTable("workflow_run");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TaskId).HasColumnName("task_id");
            entity.Property(x => x.WorkflowCode).HasColumnName("workflow_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.WorkflowVersion).HasColumnName("workflow_version").HasMaxLength(50);
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.ContextJson).HasColumnName("context_json").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.StartedAt).HasColumnName("started_at");
            entity.Property(x => x.FinishedAt).HasColumnName("finished_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
            entity.HasIndex(x => x.TaskId).HasDatabaseName("idx_workflow_run_task");
        });

        modelBuilder.Entity<WorkflowStepRun>(entity =>
        {
            entity.ToTable("workflow_step_run");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.WorkflowRunId).HasColumnName("workflow_run_id");
            entity.Property(x => x.TaskId).HasColumnName("task_id");
            entity.Property(x => x.StepId).HasColumnName("step_id").HasMaxLength(100).IsRequired();
            entity.Property(x => x.StepName).HasColumnName("step_name").HasMaxLength(200);
            entity.Property(x => x.StepType).HasColumnName("step_type").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.InputJson).HasColumnName("input_json").HasColumnType("jsonb");
            entity.Property(x => x.OutputJson).HasColumnName("output_json").HasColumnType("jsonb");
            entity.Property(x => x.ErrorMessage).HasColumnName("error_message");
            entity.Property(x => x.StartedAt).HasColumnName("started_at");
            entity.Property(x => x.FinishedAt).HasColumnName("finished_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasOne(x => x.WorkflowRun).WithMany().HasForeignKey(x => x.WorkflowRunId);
            entity.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
            entity.HasIndex(x => x.WorkflowRunId).HasDatabaseName("idx_step_run_workflow");
            entity.HasIndex(x => new { x.WorkflowRunId, x.StepId }).IsUnique().HasDatabaseName("idx_step_run_workflow_step");
        });

        modelBuilder.Entity<AgentRun>(entity =>
        {
            entity.ToTable("agent_run");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TaskId).HasColumnName("task_id");
            entity.Property(x => x.StepRunId).HasColumnName("step_run_id");
            entity.Property(x => x.AgentCode).HasColumnName("agent_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Model).HasColumnName("model").HasMaxLength(100);
            entity.Property(x => x.PromptText).HasColumnName("prompt_text");
            entity.Property(x => x.InputJson).HasColumnName("input_json").HasColumnType("jsonb");
            entity.Property(x => x.RawOutput).HasColumnName("raw_output");
            entity.Property(x => x.OutputJson).HasColumnName("output_json").HasColumnType("jsonb");
            entity.Property(x => x.SchemaValid).HasColumnName("schema_valid");
            entity.Property(x => x.TokenUsageJson).HasColumnName("token_usage_json").HasColumnType("jsonb");
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(x => x.ErrorMessage).HasColumnName("error_message");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
            entity.HasOne(x => x.StepRun).WithMany().HasForeignKey(x => x.StepRunId);
            entity.HasIndex(x => x.TaskId).HasDatabaseName("idx_agent_run_task");
        });

        modelBuilder.Entity<SkillRun>(entity =>
        {
            entity.ToTable("skill_run");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TaskId).HasColumnName("task_id");
            entity.Property(x => x.StepRunId).HasColumnName("step_run_id");
            entity.Property(x => x.SkillCode).HasColumnName("skill_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.InputJson).HasColumnName("input_json").HasColumnType("jsonb");
            entity.Property(x => x.OutputJson).HasColumnName("output_json").HasColumnType("jsonb");
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(x => x.ErrorMessage).HasColumnName("error_message");
            entity.Property(x => x.DurationMs).HasColumnName("duration_ms");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
            entity.HasOne(x => x.StepRun).WithMany().HasForeignKey(x => x.StepRunId);
            entity.HasIndex(x => x.TaskId).HasDatabaseName("idx_skill_run_task");
        });

        modelBuilder.Entity<EvidenceItem>(entity =>
        {
            entity.ToTable("evidence_item");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TaskId).HasColumnName("task_id");
            entity.Property(x => x.SourceType).HasColumnName("source_type").HasMaxLength(50).IsRequired();
            entity.Property(x => x.SourceName).HasColumnName("source_name").HasMaxLength(500);
            entity.Property(x => x.SourceUrl).HasColumnName("source_url");
            entity.Property(x => x.FileId).HasColumnName("file_id");
            entity.Property(x => x.PageNo).HasColumnName("page_no");
            entity.Property(x => x.SectionTitle).HasColumnName("section_title").HasMaxLength(500);
            entity.Property(x => x.QuoteText).HasColumnName("quote_text");
            entity.Property(x => x.ExtractedValueJson).HasColumnName("extracted_value_json").HasColumnType("jsonb");
            entity.Property(x => x.Confidence).HasColumnName("confidence").HasPrecision(5, 4);
            entity.Property(x => x.Verified).HasColumnName("verified");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
            entity.HasIndex(x => x.TaskId).HasDatabaseName("idx_evidence_task");
        });

        modelBuilder.Entity<Artifact>(entity =>
        {
            entity.ToTable("artifact");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TaskId).HasColumnName("task_id");
            entity.Property(x => x.StepRunId).HasColumnName("step_run_id");
            entity.Property(x => x.ArtifactType).HasColumnName("artifact_type").HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
            entity.Property(x => x.ContentJson).HasColumnName("content_json").HasColumnType("jsonb");
            entity.Property(x => x.FilePath).HasColumnName("file_path");
            entity.Property(x => x.Version).HasColumnName("version");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
            entity.HasOne(x => x.StepRun).WithMany().HasForeignKey(x => x.StepRunId);
            entity.HasIndex(x => x.TaskId).HasDatabaseName("idx_artifact_task");
        });

        modelBuilder.Entity<ReviewItem>(entity =>
        {
            entity.ToTable("review_item");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TaskId).HasColumnName("task_id");
            entity.Property(x => x.StepRunId).HasColumnName("step_run_id");
            entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
            entity.Property(x => x.ContentJson).HasColumnName("content_json").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.Reviewer).HasColumnName("reviewer").HasMaxLength(100);
            entity.Property(x => x.ReviewComment).HasColumnName("review_comment");
            entity.Property(x => x.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
            entity.HasOne(x => x.StepRun).WithMany().HasForeignKey(x => x.StepRunId);
            entity.HasIndex(x => new { x.TaskId, x.Status }).HasDatabaseName("idx_review_task_status");
        });

        modelBuilder.Entity<UploadedFile>(entity =>
        {
            entity.ToTable("uploaded_file");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TaskId).HasColumnName("task_id");
            entity.Property(x => x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(500).IsRequired();
            entity.Property(x => x.StoredFilePath).HasColumnName("stored_file_path").IsRequired();
            entity.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(200);
            entity.Property(x => x.FileSize).HasColumnName("file_size");
            entity.Property(x => x.ExtractedText).HasColumnName("extracted_text");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
        });

        modelBuilder.Entity<TraceEvent>(entity =>
        {
            entity.ToTable("trace_event");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TaskId).HasColumnName("task_id");
            entity.Property(x => x.WorkflowRunId).HasColumnName("workflow_run_id");
            entity.Property(x => x.StepRunId).HasColumnName("step_run_id");
            entity.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Message).HasColumnName("message");
            entity.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
            entity.HasOne(x => x.WorkflowRun).WithMany().HasForeignKey(x => x.WorkflowRunId);
            entity.HasOne(x => x.StepRun).WithMany().HasForeignKey(x => x.StepRunId);
            entity.HasIndex(x => x.TaskId).HasDatabaseName("idx_trace_task");
        });

        modelBuilder.Entity<WorkflowTemplate>(entity =>
        {
            entity.ToTable("workflow_template");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ScenarioCode).HasColumnName("scenario_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
            entity.Property(x => x.Version).HasColumnName("version").HasMaxLength(50).IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.InputSchemaJson).HasColumnName("input_schema_json").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasMany(x => x.Steps).WithOne(x => x.WorkflowTemplate).HasForeignKey(x => x.WorkflowTemplateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.ScenarioCode, x.IsActive }).HasDatabaseName("idx_workflow_template_scenario_active");
        });

        modelBuilder.Entity<WorkflowStepTemplate>(entity =>
        {
            entity.ToTable("workflow_step_template");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.WorkflowTemplateId).HasColumnName("workflow_template_id");
            entity.Property(x => x.StepId).HasColumnName("step_id").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
            entity.Property(x => x.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
            entity.Property(x => x.SkillCode).HasColumnName("skill_code").HasMaxLength(100);
            entity.Property(x => x.AgentCode).HasColumnName("agent_code").HasMaxLength(100);
            entity.Property(x => x.DependsOnJson).HasColumnName("depends_on_json").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.Property(x => x.DataSourceBindingsJson).HasColumnName("data_source_bindings_json").HasColumnType("jsonb").IsRequired();
            entity.HasIndex(x => x.WorkflowTemplateId).HasDatabaseName("idx_step_template_workflow");
        });

        modelBuilder.Entity<AgentTemplate>(entity =>
        {
            entity.ToTable("agent_template");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ScenarioCode).HasColumnName("scenario_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.AgentCode).HasColumnName("agent_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.Model).HasColumnName("model").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Temperature).HasColumnName("temperature").HasPrecision(5, 4);
            entity.Property(x => x.SystemPrompt).HasColumnName("system_prompt").IsRequired();
            entity.Property(x => x.OutputSchemaJson).HasColumnName("output_schema_json").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.AllowedSkillsJson).HasColumnName("allowed_skills_json").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.AllowedDataSourcesJson).HasColumnName("allowed_data_sources_json").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.MaxToolCalls).HasColumnName("max_tool_calls");
            entity.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => new { x.ScenarioCode, x.AgentCode }).HasDatabaseName("idx_agent_template_scenario_code");
        });
    }
}
