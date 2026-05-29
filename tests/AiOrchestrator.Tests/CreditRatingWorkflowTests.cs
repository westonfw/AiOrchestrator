using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AiOrchestrator.Tests;

public sealed class CreditRatingWorkflowTests
{
    [Fact]
    public async Task CreditRatingWorkflow_PausesForReview_AndContinuesToMarkdownArtifact()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Database:UseInMemory", "true");
                builder.UseSetting("Database:InMemoryName", Guid.NewGuid().ToString("N"));
            });
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/tasks", new
        {
            scenarioCode = "credit_rating",
            title = "测试科技信评报告",
            input = new
            {
                company_name = "测试科技股份有限公司",
                period = "2024",
                report_type = "主体信用分析",
                materials_text = "测试科技股份有限公司主营企业软件。2024年营业收入 12 亿元，净利润 1.2 亿元，总资产 30 亿元，总负债 16 亿元，流动资产 10 亿元，流动负债 8 亿元。"
            }
        });
        createResponse.EnsureSuccessStatusCode();
        var taskId = await ReadDataPropertyAsync<Guid>(createResponse, "id");

        var startResponse = await client.PostAsync($"/api/tasks/{taskId}/start", null);
        Assert.True(startResponse.IsSuccessStatusCode, await startResponse.Content.ReadAsStringAsync());

        var detailAtReviewResponse = await client.GetAsync($"/api/tasks/{taskId}");
        var detailAtReviewText = await detailAtReviewResponse.Content.ReadAsStringAsync();
        var detailAtReview = JsonSerializer.Deserialize<JsonElement>(detailAtReviewText);
        Assert.True(
            detailAtReview.GetProperty("data").GetProperty("task").GetProperty("status").GetString() == "WaitingReview",
            detailAtReviewText);
        Assert.Contains(detailAtReview.GetProperty("data").GetProperty("steps").EnumerateArray(), step =>
            step.GetProperty("stepId").GetString() == "human_review"
            && step.GetProperty("status").GetString() == "WaitingReview");

        var reviews = await client.GetFromJsonAsync<JsonElement>("/api/reviews?status=Pending");
        var reviewId = reviews.GetProperty("data").GetProperty("items").EnumerateArray().Single().GetProperty("id").GetGuid();

        var approveResponse = await client.PostAsJsonAsync($"/api/reviews/{reviewId}/approve", new
        {
            comment = "同意生成草稿"
        });
        approveResponse.EnsureSuccessStatusCode();

        var finalDetail = await client.GetFromJsonAsync<JsonElement>($"/api/tasks/{taskId}");
        var data = finalDetail.GetProperty("data");
        Assert.Equal("Succeeded", data.GetProperty("task").GetProperty("status").GetString());
        Assert.Contains(data.GetProperty("steps").EnumerateArray(), step =>
            step.GetProperty("stepId").GetString() == "generate_report"
            && step.GetProperty("status").GetString() == "Succeeded");
        Assert.Contains(data.GetProperty("artifacts").EnumerateArray(), artifact =>
            artifact.GetProperty("artifactType").GetString() == "Markdown");
        Assert.NotEmpty(data.GetProperty("evidence").EnumerateArray());
    }

    private static async Task<T> ReadDataPropertyAsync<T>(HttpResponseMessage response, string propertyName)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.GetProperty("data").GetProperty(propertyName).Deserialize<T>()!;
    }
}
