using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    agent_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    temperature = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    system_prompt = table.Column<string>(type: "text", nullable: false),
                    output_schema_json = table.Column<string>(type: "jsonb", nullable: false),
                    allowed_skills_json = table.Column<string>(type: "jsonb", nullable: false),
                    allowed_data_sources_json = table.Column<string>(type: "jsonb", nullable: false),
                    max_tool_calls = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_template", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    input_schema_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_template", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_step_template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    skill_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    agent_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    depends_on_json = table.Column<string>(type: "jsonb", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    data_source_bindings_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_step_template", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_step_template_workflow_template_workflow_template_~",
                        column: x => x.workflow_template_id,
                        principalTable: "workflow_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_agent_template_scenario_code",
                table: "agent_template",
                columns: new[] { "scenario_code", "agent_code" });

            migrationBuilder.CreateIndex(
                name: "idx_step_template_workflow",
                table: "workflow_step_template",
                column: "workflow_template_id");

            migrationBuilder.CreateIndex(
                name: "idx_workflow_template_scenario_active",
                table: "workflow_template",
                columns: new[] { "scenario_code", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_template");

            migrationBuilder.DropTable(
                name: "workflow_step_template");

            migrationBuilder.DropTable(
                name: "workflow_template");
        }
    }
}
