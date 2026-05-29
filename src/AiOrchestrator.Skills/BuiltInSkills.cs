using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AiOrchestrator.Application;

namespace AiOrchestrator.Skills;

public abstract class AiSkillBase : IAiSkill
{
    public abstract string Code { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public JsonNode InputSchema { get; } = JsonNode.Parse("""{"type":"object"}""")!;
    public JsonNode OutputSchema { get; } = JsonNode.Parse("""{"type":"object"}""")!;
    public abstract Task<SkillResult> ExecuteAsync(SkillContext context, JsonNode input, CancellationToken ct);

    protected static JsonObject InputObject(JsonNode input)
    {
        var context = input.AsObject();
        return context["input"]?.AsObject() ?? new JsonObject();
    }

    protected static JsonObject StepsObject(JsonNode input)
    {
        var context = input.AsObject();
        return context["steps"]?.AsObject() ?? new JsonObject();
    }
}

public sealed class CompanyResolutionSkill : AiSkillBase
{
    public override string Code => "company_resolution";
    public override string Name => "公司主体识别";
    public override string Description => "规范化用户输入的公司名称。";

    public override Task<SkillResult> ExecuteAsync(SkillContext context, JsonNode input, CancellationToken ct)
    {
        var companyName = InputObject(input)["company_name"]?.GetValue<string>()?.Trim();
        if (string.IsNullOrWhiteSpace(companyName))
        {
            companyName = "未命名公司";
        }

        var output = new JsonObject
        {
            ["company_name"] = companyName,
            ["normalized_name"] = companyName,
            ["resolution_method"] = "deterministic_normalization",
            ["confidence"] = 0.95
        };

        return Task.FromResult(new SkillResult { Success = true, Output = output });
    }
}

public class CollectMaterialsSkill : AiSkillBase
{
    public override string Code => "collect_materials";
    public override string Name => "收集任务资料";
    public override string Description => "读取用户粘贴的文本资料，第一版不自动联网。";

    public override Task<SkillResult> ExecuteAsync(SkillContext context, JsonNode input, CancellationToken ct)
    {
        var taskInput = InputObject(input);
        var materialsText = taskInput["materials_text"]?.GetValue<string>() ?? string.Empty;
        var output = new JsonObject
        {
            ["materials_text"] = materialsText,
            ["material_count"] = string.IsNullOrWhiteSpace(materialsText) ? 0 : 1,
            ["source_type"] = "uploaded_text"
        };

        var result = new SkillResult { Success = true, Output = output };
        if (!string.IsNullOrWhiteSpace(materialsText))
        {
            result.EvidenceItems.Add(new EvidenceDraft
            {
                SourceType = "uploaded_text",
                SourceName = "用户粘贴资料",
                SectionTitle = "原始资料",
                QuoteText = Truncate(materialsText, 800),
                ExtractedValue = new JsonObject { ["field"] = "materials_text" },
                Confidence = 0.9m
            });
        }

        return Task.FromResult(result);
    }

    protected static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
}

public sealed class ParseUploadedFileSkill : CollectMaterialsSkill
{
    public override string Code => "parse_uploaded_file";
    public override string Name => "解析上传文件";
    public override string Description => "第一版复用文本资料解析能力，后续扩展 PDF/Excel。";
}

public sealed class ExtractBasicFactsSkill : AiSkillBase
{
    public override string Code => "extract_basic_facts";
    public override string Name => "提取基础事实";
    public override string Description => "从资料文本中确定性提取基础公司事实和财务字段。";

    public override Task<SkillResult> ExecuteAsync(SkillContext context, JsonNode input, CancellationToken ct)
    {
        var taskInput = InputObject(input);
        var steps = StepsObject(input);
        var materialsText = steps["collect_materials"]?["materials_text"]?.GetValue<string>()
            ?? taskInput["materials_text"]?.GetValue<string>()
            ?? string.Empty;
        var companyName = steps["resolve_company"]?["normalized_name"]?.GetValue<string>()
            ?? taskInput["company_name"]?.GetValue<string>()
            ?? "未命名公司";
        var period = taskInput["period"]?.GetValue<string>() ?? "unknown";

        var facts = new JsonObject
        {
            ["company_name"] = companyName,
            ["period"] = period,
            ["business_summary"] = ExtractBusinessSummary(materialsText)
        };

        var financials = new JsonObject();
        var result = new SkillResult { Success = true };
        AddNumberIfFound(financials, result, materialsText, "revenue", "营业收入|收入|营收");
        AddNumberIfFound(financials, result, materialsText, "net_profit", "净利润|归母净利润|利润");
        AddNumberIfFound(financials, result, materialsText, "total_assets", "总资产|资产总额");
        AddNumberIfFound(financials, result, materialsText, "total_liabilities", "总负债|负债总额");
        AddNumberIfFound(financials, result, materialsText, "current_assets", "流动资产");
        AddNumberIfFound(financials, result, materialsText, "current_liabilities", "流动负债");
        AddNumberIfFound(financials, result, materialsText, "cash", "货币资金|现金");

        facts["financials"] = JsonSupport.CloneNode(financials);
        result.Output = new JsonObject
        {
            ["company_profile"] = facts,
            ["financials"] = JsonSupport.CloneNode(financials)
        };

        return Task.FromResult(result);
    }

    private static void AddNumberIfFound(JsonObject financials, SkillResult result, string text, string field, string labelPattern)
    {
        var match = Regex.Match(text, $@"({labelPattern})[^\d\-]{{0,12}}(?<value>-?\d+(?:\.\d+)?)\s*(?<unit>亿元|万元|元)?", RegexOptions.IgnoreCase);
        if (!match.Success || !decimal.TryParse(match.Groups["value"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            return;
        }

        var unit = match.Groups["unit"].Success ? match.Groups["unit"].Value : "unknown";
        var normalized = NormalizeCny(value, unit);
        financials[field] = normalized;
        result.EvidenceItems.Add(new EvidenceDraft
        {
            SourceType = "uploaded_text",
            SourceName = "用户粘贴资料",
            SectionTitle = field,
            QuoteText = match.Value,
            ExtractedValue = new JsonObject
            {
                ["field"] = field,
                ["value"] = normalized,
                ["unit"] = "CNY"
            },
            Confidence = 0.82m
        });
    }

    private static decimal NormalizeCny(decimal value, string unit)
    {
        return unit switch
        {
            "亿元" => value * 100_000_000m,
            "万元" => value * 10_000m,
            _ => value
        };
    }

    private static string ExtractBusinessSummary(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "资料未提供主营业务描述。";
        }

        var compact = Regex.Replace(text, @"\s+", " ").Trim();
        return compact.Length <= 160 ? compact : compact[..160];
    }
}

public sealed class CalculateFinancialRatiosSkill : AiSkillBase
{
    public override string Code => "calculate_financial_ratios";
    public override string Name => "计算财务指标";
    public override string Description => "由确定性代码计算资产负债率、流动比率、净利率等指标。";

    public override Task<SkillResult> ExecuteAsync(SkillContext context, JsonNode input, CancellationToken ct)
    {
        var financials = StepsObject(input)["extract_basic_facts"]?["financials"]?.AsObject() ?? new JsonObject();
        var ratios = new JsonObject();

        AddRatio(ratios, "asset_liability_ratio", GetNumber(financials, "total_liabilities"), GetNumber(financials, "total_assets"));
        AddRatio(ratios, "current_ratio", GetNumber(financials, "current_assets"), GetNumber(financials, "current_liabilities"));
        AddRatio(ratios, "net_margin", GetNumber(financials, "net_profit"), GetNumber(financials, "revenue"));

        var output = new JsonObject
        {
            ["financials"] = JsonSupport.CloneNode(financials) ?? new JsonObject(),
            ["ratios"] = ratios,
            ["calculation_method"] = "deterministic"
        };

        return Task.FromResult(new SkillResult { Success = true, Output = output });
    }

    private static decimal? GetNumber(JsonObject financials, string key)
    {
        return financials.TryGetPropertyValue(key, out var node) && node is not null && decimal.TryParse(node.ToString(), out var value)
            ? value
            : null;
    }

    private static void AddRatio(JsonObject ratios, string name, decimal? numerator, decimal? denominator)
    {
        ratios[name] = numerator.HasValue && denominator is > 0
            ? Math.Round(numerator.Value / denominator.Value, 4)
            : null;
    }
}

public class GenerateMarkdownReportSkill : AiSkillBase
{
    public override string Code => "generate_markdown_report";
    public override string Name => "生成 Markdown 报告";
    public override string Description => "根据结构化上下文生成 Markdown 信评报告草稿。";

    public override Task<SkillResult> ExecuteAsync(SkillContext context, JsonNode input, CancellationToken ct)
    {
        var taskInput = InputObject(input);
        var steps = StepsObject(input);
        var companyName = steps["resolve_company"]?["normalized_name"]?.GetValue<string>()
            ?? taskInput["company_name"]?.GetValue<string>()
            ?? "未命名公司";

        var markdown = new StringBuilder();
        markdown.AppendLine($"# {companyName} 信用分析报告草稿");
        markdown.AppendLine();
        markdown.AppendLine("> 本报告为 AI 辅助生成的内部草稿，需人工审核后方可作为正式材料使用。");
        markdown.AppendLine();
        AppendCompanyProfile(markdown, steps["extract_basic_facts"]?["company_profile"]);
        AppendFinancialAnalysis(markdown, steps["financial_analysis"], steps["calculate_financial_ratios"]);
        AppendIndustryAnalysis(markdown, steps["industry_analysis"]);
        AppendRiskAnalysis(markdown, steps["risk_analysis"]);
        AppendRatingOpinion(markdown, steps["rating_committee"]);
        AppendDevilReview(markdown, steps["devil_review"]);
        AppendHumanReview(markdown, steps["human_review"]);
        markdown.AppendLine("## 八、证据列表");
        markdown.AppendLine();
        markdown.AppendLine("证据明细请以 evidence API 返回为准。");

        var output = new JsonObject
        {
            ["report_name"] = $"{companyName} 信用分析报告草稿",
            ["format"] = "markdown",
            ["markdown"] = markdown.ToString()
        };

        return Task.FromResult(new SkillResult
        {
            Success = true,
            Output = output,
            Artifacts =
            {
                new ArtifactDraft
                {
                    ArtifactType = "markdown",
                    Name = $"{companyName} 信用分析报告草稿.md",
                    Content = output
                }
            }
        });
    }

    private static void AppendCompanyProfile(StringBuilder markdown, JsonNode? profile)
    {
        markdown.AppendLine("## 一、公司概况");
        markdown.AppendLine();
        markdown.AppendLine($"- 公司名称：{Text(profile?["company_name"])}");
        markdown.AppendLine($"- 报告期间：{Text(profile?["period"])}");
        markdown.AppendLine($"- 业务摘要：{Text(profile?["business_summary"])}");
        if (profile?["financials"] is JsonObject financials)
        {
            markdown.AppendLine("- 已提取财务字段：");
            foreach (var (key, value) in financials)
            {
                markdown.AppendLine($"  - {Label(key)}：{Money(value)}");
            }
        }

        markdown.AppendLine();
    }

    private static void AppendFinancialAnalysis(StringBuilder markdown, JsonNode? analysis, JsonNode? ratioStep)
    {
        markdown.AppendLine("## 二、财务分析");
        markdown.AppendLine();
        markdown.AppendLine(Text(analysis?["summary"]));
        AppendClaims(markdown, "优势", analysis?["strengths"]);
        AppendClaims(markdown, "弱项", analysis?["weaknesses"]);
        if (ratioStep?["ratios"] is JsonObject ratios)
        {
            markdown.AppendLine("### 关键财务指标");
            foreach (var (key, value) in ratios)
            {
                markdown.AppendLine($"- {Label(key)}：{Ratio(value)}");
            }
            markdown.AppendLine();
        }
        AppendStringList(markdown, "不确定事项", analysis?["uncertainties"]);
    }

    private static void AppendIndustryAnalysis(StringBuilder markdown, JsonNode? analysis)
    {
        markdown.AppendLine("## 三、行业与经营分析");
        markdown.AppendLine();
        markdown.AppendLine(Text(analysis?["summary"]));
        markdown.AppendLine($"- 行业位置：{Text(analysis?["industry_position"])}");
        AppendStringList(markdown, "机会", analysis?["opportunities"]);
        AppendStringList(markdown, "行业风险", analysis?["risks"]);
        AppendStringList(markdown, "不确定事项", analysis?["uncertainties"]);
    }

    private static void AppendRiskAnalysis(StringBuilder markdown, JsonNode? analysis)
    {
        markdown.AppendLine("## 四、主要风险因素");
        markdown.AppendLine();
        markdown.AppendLine(Text(analysis?["summary"]));
        markdown.AppendLine($"- 综合风险等级：{Text(analysis?["overall_risk_level"])}");
        if (analysis?["risks"] is JsonArray risks)
        {
            foreach (var risk in risks)
            {
                markdown.AppendLine($"- {Text(risk?["risk_type"])}（{Text(risk?["severity"])}）：{Text(risk?["reason"])} {EvidenceText(risk?["evidence_ids"])}");
            }
            markdown.AppendLine();
        }
        AppendStringList(markdown, "不确定事项", analysis?["uncertainties"]);
    }

    private static void AppendRatingOpinion(StringBuilder markdown, JsonNode? opinion)
    {
        markdown.AppendLine("## 五、初步评级意见");
        markdown.AppendLine();
        markdown.AppendLine(Text(opinion?["summary"]));
        markdown.AppendLine($"- 评级方向：{Text(opinion?["rating_direction"])}");
        markdown.AppendLine($"- 需要人工审核：{Text(opinion?["requires_human_review"])}");
        AppendStringList(markdown, "支持因素", opinion?["supporting_factors"]);
        AppendStringList(markdown, "约束因素", opinion?["constraint_factors"]);
    }

    private static void AppendDevilReview(StringBuilder markdown, JsonNode? review)
    {
        markdown.AppendLine("## 六、反方审查意见");
        markdown.AppendLine();
        markdown.AppendLine(Text(review?["summary"]));
        AppendStringList(markdown, "反方异议", review?["objections"]);
        AppendStringList(markdown, "缺失证据", review?["missing_evidence"]);
        AppendStringList(markdown, "建议人工复核点", review?["suggested_review_points"]);
    }

    private static void AppendHumanReview(StringBuilder markdown, JsonNode? review)
    {
        markdown.AppendLine("## 七、人工审核");
        markdown.AppendLine();
        markdown.AppendLine($"- 审核状态：{Text(review?["status"])}");
        markdown.AppendLine($"- 审核意见：{Text(review?["comment"])}");
        markdown.AppendLine();
    }

    private static void AppendClaims(StringBuilder markdown, string title, JsonNode? claims)
    {
        if (claims is not JsonArray array || array.Count == 0)
        {
            return;
        }

        markdown.AppendLine($"### {title}");
        foreach (var item in array)
        {
            markdown.AppendLine($"- {Text(item?["claim"])}（置信度：{Ratio(item?["confidence"])}）{EvidenceText(item?["evidence_ids"])}");
        }
        markdown.AppendLine();
    }

    private static void AppendStringList(StringBuilder markdown, string title, JsonNode? values)
    {
        if (values is not JsonArray array || array.Count == 0)
        {
            return;
        }

        markdown.AppendLine($"### {title}");
        foreach (var item in array)
        {
            markdown.AppendLine($"- {Text(item)}");
        }
        markdown.AppendLine();
    }

    private static string Text(JsonNode? node)
    {
        if (node is null)
        {
            return "未提供";
        }

        if (node is JsonValue)
        {
            return node.ToString();
        }

        return node.ToJsonString(JsonSupport.SerializerOptions);
    }

    private static string EvidenceText(JsonNode? node)
    {
        if (node is not JsonArray ids || ids.Count == 0)
        {
            return string.Empty;
        }

        return $"（证据：{string.Join(", ", ids.Select(Text))}）";
    }

    private static string Money(JsonNode? node)
    {
        return decimal.TryParse(node?.ToString(), out var value) ? $"{value / 100_000_000m:0.##} 亿元" : "未提取";
    }

    private static string Ratio(JsonNode? node)
    {
        return decimal.TryParse(node?.ToString(), out var value) ? $"{value:0.####}" : "未计算";
    }

    private static string Label(string key)
    {
        return key switch
        {
            "revenue" => "营业收入",
            "net_profit" => "净利润",
            "total_assets" => "总资产",
            "total_liabilities" => "总负债",
            "current_assets" => "流动资产",
            "current_liabilities" => "流动负债",
            "cash" => "货币资金",
            "asset_liability_ratio" => "资产负债率",
            "current_ratio" => "流动比率",
            "net_margin" => "净利率",
            _ => key
        };
    }
}

public sealed class GenerateCreditReportDocxSkill : GenerateMarkdownReportSkill
{
    public override string Code => "generate_credit_report_docx";
    public override string Name => "生成 DOCX 报告占位";
    public override string Description => "第一版先生成 Markdown 内容，保留 DOCX 技能入口。";
}
