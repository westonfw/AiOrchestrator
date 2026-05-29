using System.Text.Json.Nodes;
using AiOrchestrator.Application;

namespace AiOrchestrator.Agents;

public sealed class MockLlmProvider : ILlmProvider
{
    public Task<LlmResult> GenerateJsonAsync(LlmRequest request, CancellationToken ct)
    {
        var evidenceIds = FindEvidenceIds(request.Input).Take(3).ToArray();
        var output = request.AgentCode switch
        {
            "financial_analyst" => FinancialAnalysis(request.Input, evidenceIds),
            "industry_analyst" => IndustryAnalysis(),
            "risk_analyst" => RiskAnalysis(evidenceIds),
            "rating_committee" => RatingOpinion(),
            "devil_advocate" => DevilReview(),
            _ => new JsonObject { ["summary"] = $"Mock output for {request.AgentCode}" }
        };

        return Task.FromResult(new LlmResult
        {
            JsonOutput = output,
            RawOutput = output.ToJsonString(JsonSupport.SerializerOptions),
            TokenUsageJson = """{"prompt_tokens":0,"completion_tokens":0,"total_tokens":0}"""
        });
    }

    private static JsonObject FinancialAnalysis(JsonNode? input, string[] evidenceIds)
    {
        var ratios = input?["steps"]?["calculate_financial_ratios"]?["ratios"]?.AsObject();
        return new JsonObject
        {
            ["summary"] = "基于已提取资料和确定性财务指标，收入、利润与偿债能力需结合原始证据继续复核。",
            ["strengths"] = new JsonArray
            {
                Claim("已完成财务字段和核心比率的结构化计算。", evidenceIds, 0.72)
            },
            ["weaknesses"] = new JsonArray
            {
                Claim("部分财务字段可能缺失，影响完整偿债能力判断。", evidenceIds, 0.66)
            },
            ["key_ratios"] = JsonSupport.CloneNode(ratios) ?? new JsonObject(),
            ["risk_level"] = "medium",
            ["uncertainties"] = new JsonArray { "Mock 模式未接入外部审计报告校验。" }
        };
    }

    private static JsonObject IndustryAnalysis()
    {
        return new JsonObject
        {
            ["summary"] = "行业判断仅基于用户提交资料，暂不自动联网补充行业数据。",
            ["industry_position"] = "unknown",
            ["opportunities"] = new JsonArray { "如资料显示主营业务稳定，可作为后续人工复核点。" },
            ["risks"] = new JsonArray { "行业景气度、竞争格局和政策影响证据不足。" },
            ["uncertainties"] = new JsonArray { "缺少外部行业数据源。" }
        };
    }

    private static JsonObject RiskAnalysis(string[] evidenceIds)
    {
        return new JsonObject
        {
            ["summary"] = "当前主要风险来自资料完整性、短期流动性和证据覆盖不足。",
            ["risks"] = new JsonArray
            {
                new JsonObject
                {
                    ["risk_type"] = "liquidity",
                    ["severity"] = "medium",
                    ["reason"] = "短期偿债能力需结合流动资产、流动负债与现金流资料复核。",
                    ["evidence_ids"] = ToJsonArray(evidenceIds)
                }
            },
            ["overall_risk_level"] = "medium",
            ["uncertainties"] = new JsonArray { "未核验诉讼、担保和外部舆情信息。" }
        };
    }

    private static JsonObject RatingOpinion()
    {
        return new JsonObject
        {
            ["summary"] = "建议形成内部稳定方向草稿，但需人工审核证据充分性后方可定稿。",
            ["rating_direction"] = "stable",
            ["supporting_factors"] = new JsonArray { "工作流已完成财务、行业和风险结构化分析。" },
            ["constraint_factors"] = new JsonArray { "资料来源单一，缺少外部审计和行业校验。" },
            ["requires_human_review"] = true
        };
    }

    private static JsonObject DevilReview()
    {
        return new JsonObject
        {
            ["summary"] = "当前评级意见仍存在证据不足和外部校验不足的问题。",
            ["objections"] = new JsonArray { "不应把 Mock 分析视作正式评级结论。", "行业与风险判断需要更多来源支撑。" },
            ["missing_evidence"] = new JsonArray { "审计报告全文", "债务期限结构", "现金流量表", "行业可比数据" },
            ["suggested_review_points"] = new JsonArray { "人工确认关键财务字段", "核验重大风险事项", "确认报告仅为草稿" }
        };
    }

    private static JsonObject Claim(string claim, string[] evidenceIds, double confidence)
    {
        return new JsonObject
        {
            ["claim"] = claim,
            ["evidence_ids"] = ToJsonArray(evidenceIds),
            ["confidence"] = confidence
        };
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    private static IEnumerable<string> FindEvidenceIds(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var (key, value) in obj)
            {
                if (key == "created_evidence_ids" && value is JsonArray array)
                {
                    foreach (var item in array)
                    {
                        if (item is not null)
                        {
                            yield return item.GetValue<string>();
                        }
                    }
                }

                foreach (var child in FindEvidenceIds(value))
                {
                    yield return child;
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                foreach (var child in FindEvidenceIds(item))
                {
                    yield return child;
                }
            }
        }
    }
}
