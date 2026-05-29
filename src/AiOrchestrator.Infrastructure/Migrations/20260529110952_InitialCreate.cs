using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_task",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    input_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    current_step = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_task", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "evidence_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    source_url = table.Column<string>(type: "text", nullable: true),
                    file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    page_no = table.Column<int>(type: "integer", nullable: true),
                    section_title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    quote_text = table.Column<string>(type: "text", nullable: true),
                    extracted_value_json = table.Column<string>(type: "jsonb", nullable: true),
                    confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    verified = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_evidence_item_ai_task_task_id",
                        column: x => x.task_id,
                        principalTable: "ai_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "uploaded_file",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    original_file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    stored_file_path = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    extracted_text = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uploaded_file", x => x.id);
                    table.ForeignKey(
                        name: "FK_uploaded_file_ai_task_task_id",
                        column: x => x.task_id,
                        principalTable: "ai_task",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "workflow_run",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    workflow_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    context_json = table.Column<string>(type: "jsonb", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_run", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_run_ai_task_task_id",
                        column: x => x.task_id,
                        principalTable: "ai_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_step_run",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    step_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    step_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    input_json = table.Column<string>(type: "jsonb", nullable: true),
                    output_json = table.Column<string>(type: "jsonb", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_step_run", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_step_run_ai_task_task_id",
                        column: x => x.task_id,
                        principalTable: "ai_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workflow_step_run_workflow_run_workflow_run_id",
                        column: x => x.workflow_run_id,
                        principalTable: "workflow_run",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_run",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    prompt_text = table.Column<string>(type: "text", nullable: true),
                    input_json = table.Column<string>(type: "jsonb", nullable: true),
                    raw_output = table.Column<string>(type: "text", nullable: true),
                    output_json = table.Column<string>(type: "jsonb", nullable: true),
                    schema_valid = table.Column<bool>(type: "boolean", nullable: false),
                    token_usage_json = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_run", x => x.id);
                    table.ForeignKey(
                        name: "FK_agent_run_ai_task_task_id",
                        column: x => x.task_id,
                        principalTable: "ai_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_agent_run_workflow_step_run_step_run_id",
                        column: x => x.step_run_id,
                        principalTable: "workflow_step_run",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artifact",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    artifact_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content_json = table.Column<string>(type: "jsonb", nullable: true),
                    file_path = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artifact", x => x.id);
                    table.ForeignKey(
                        name: "FK_artifact_ai_task_task_id",
                        column: x => x.task_id,
                        principalTable: "ai_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_artifact_workflow_step_run_step_run_id",
                        column: x => x.step_run_id,
                        principalTable: "workflow_step_run",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "review_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reviewer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    review_comment = table.Column<string>(type: "text", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_review_item_ai_task_task_id",
                        column: x => x.task_id,
                        principalTable: "ai_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_review_item_workflow_step_run_step_run_id",
                        column: x => x.step_run_id,
                        principalTable: "workflow_step_run",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skill_run",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    input_json = table.Column<string>(type: "jsonb", nullable: true),
                    output_json = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_run", x => x.id);
                    table.ForeignKey(
                        name: "FK_skill_run_ai_task_task_id",
                        column: x => x.task_id,
                        principalTable: "ai_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_skill_run_workflow_step_run_step_run_id",
                        column: x => x.step_run_id,
                        principalTable: "workflow_step_run",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trace_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workflow_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    step_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    message = table.Column<string>(type: "text", nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trace_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_trace_event_ai_task_task_id",
                        column: x => x.task_id,
                        principalTable: "ai_task",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_trace_event_workflow_run_workflow_run_id",
                        column: x => x.workflow_run_id,
                        principalTable: "workflow_run",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_trace_event_workflow_step_run_step_run_id",
                        column: x => x.step_run_id,
                        principalTable: "workflow_step_run",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_agent_run_task",
                table: "agent_run",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_agent_run_step_run_id",
                table: "agent_run",
                column: "step_run_id");

            migrationBuilder.CreateIndex(
                name: "idx_ai_task_scenario",
                table: "ai_task",
                column: "scenario_code");

            migrationBuilder.CreateIndex(
                name: "idx_ai_task_status",
                table: "ai_task",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_artifact_task",
                table: "artifact",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_artifact_step_run_id",
                table: "artifact",
                column: "step_run_id");

            migrationBuilder.CreateIndex(
                name: "idx_evidence_task",
                table: "evidence_item",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "idx_review_task_status",
                table: "review_item",
                columns: new[] { "task_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_review_item_step_run_id",
                table: "review_item",
                column: "step_run_id");

            migrationBuilder.CreateIndex(
                name: "idx_skill_run_task",
                table: "skill_run",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_skill_run_step_run_id",
                table: "skill_run",
                column: "step_run_id");

            migrationBuilder.CreateIndex(
                name: "idx_trace_task",
                table: "trace_event",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_trace_event_step_run_id",
                table: "trace_event",
                column: "step_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_trace_event_workflow_run_id",
                table: "trace_event",
                column: "workflow_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_uploaded_file_task_id",
                table: "uploaded_file",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "idx_workflow_run_task",
                table: "workflow_run",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "idx_step_run_workflow",
                table: "workflow_step_run",
                column: "workflow_run_id");

            migrationBuilder.CreateIndex(
                name: "idx_step_run_workflow_step",
                table: "workflow_step_run",
                columns: new[] { "workflow_run_id", "step_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_step_run_task_id",
                table: "workflow_step_run",
                column: "task_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_run");

            migrationBuilder.DropTable(
                name: "artifact");

            migrationBuilder.DropTable(
                name: "evidence_item");

            migrationBuilder.DropTable(
                name: "review_item");

            migrationBuilder.DropTable(
                name: "skill_run");

            migrationBuilder.DropTable(
                name: "trace_event");

            migrationBuilder.DropTable(
                name: "uploaded_file");

            migrationBuilder.DropTable(
                name: "workflow_step_run");

            migrationBuilder.DropTable(
                name: "workflow_run");

            migrationBuilder.DropTable(
                name: "ai_task");
        }
    }
}
