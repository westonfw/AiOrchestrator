using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AiOrchestrator.Application;
using Microsoft.Extensions.Configuration;

namespace AiOrchestrator.Agents;

public sealed class OpenAICompatibleLlmProvider : ILlmProvider, IDisposable
{
    private static readonly SemaphoreSlim LogWriteLock = new(1, 1);

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string? _apiKey;
    private readonly string _defaultModel;
    private readonly bool _useJsonResponseFormat;
    private readonly bool _logRequests;
    private readonly string _logDirectory;

    public OpenAICompatibleLlmProvider(IConfiguration configuration)
    {
        _baseUrl = TrimTrailingSlash(configuration["Llm:BaseUrl"] ?? "https://api.openai.com/v1");
        _apiKey = configuration["Llm:ApiKey"] ?? Environment.GetEnvironmentVariable("LLM_API_KEY");
        _defaultModel = configuration["Llm:DefaultModel"] ?? "gpt-4.1-mini";
        _useJsonResponseFormat = !bool.TryParse(configuration["Llm:UseJsonResponseFormat"], out var useJsonResponseFormat)
            || useJsonResponseFormat;
        _logRequests = !bool.TryParse(configuration["Llm:LogRequests"], out var logRequests) || logRequests;
        _logDirectory = configuration["Llm:LogDirectory"] ?? Path.Combine("logs", "llm");

        var timeoutSeconds = int.TryParse(configuration["Llm:TimeoutSeconds"], out var configuredTimeout)
            ? configuredTimeout
            : 120;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
    }

    public async Task<LlmResult> GenerateJsonAsync(LlmRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Llm:ApiKey is required when Llm:Provider is OpenAICompatible.");
        }

        var endpoint = $"{_baseUrl}/chat/completions";
        var payload = BuildPayload(request);
        var startedAt = DateTimeOffset.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int? statusCode = null;
        string? responseText = null;
        string? errorMessage = null;

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            httpRequest.Content = new StringContent(payload.ToJsonString(JsonSupport.SerializerOptions), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            statusCode = (int)response.StatusCode;
            responseText = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"LLM request failed with HTTP {(int)response.StatusCode}: {SanitizeError(responseText)}");
            }

            var root = JsonNode.Parse(responseText)?.AsObject()
                ?? throw new InvalidOperationException("LLM response was not valid JSON.");
            var content = root["choices"]?[0]?["message"]?["content"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("LLM response did not contain message content.");
            }

            var jsonOutput = ParseJsonContent(content);
            return new LlmResult
            {
                RawOutput = content,
                JsonOutput = jsonOutput,
                TokenUsageJson = JsonSupport.ToJson(root["usage"])
            };
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            await WriteCallLogAsync(
                request,
                endpoint,
                payload,
                startedAt,
                stopwatch.ElapsedMilliseconds,
                statusCode,
                responseText,
                errorMessage,
                CancellationToken.None);
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private JsonObject BuildPayload(LlmRequest request)
    {
        var messages = new JsonArray();
        foreach (var message in request.Messages)
        {
            messages.Add(new JsonObject
            {
                ["role"] = message.Role,
                ["content"] = message.Content
            });
        }

        if (request.OutputSchema is not null)
        {
            messages.Add(new JsonObject
            {
                ["role"] = "user",
                ["content"] = "Output must be a single valid JSON object matching this JSON Schema. Do not wrap it in Markdown fences.\n"
                    + request.OutputSchema.ToJsonString(JsonSupport.SerializerOptions)
            });
        }

        var model = string.Equals(request.Model, "mock", StringComparison.OrdinalIgnoreCase)
            || string.Equals(request.Model, "default", StringComparison.OrdinalIgnoreCase)
            ? _defaultModel
            : request.Model;

        var payload = new JsonObject
        {
            ["model"] = model,
            ["temperature"] = request.Temperature,
            ["messages"] = messages
        };

        if (_useJsonResponseFormat)
        {
            payload["response_format"] = new JsonObject
            {
                ["type"] = "json_object"
            };
        }

        return payload;
    }

    private static JsonNode ParseJsonContent(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var match = Regex.Match(trimmed, @"^```(?:json)?\s*(?<json>[\s\S]*?)\s*```$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                trimmed = match.Groups["json"].Value.Trim();
            }
        }

        return JsonNode.Parse(trimmed)
            ?? throw new InvalidOperationException("LLM content did not contain a JSON payload.");
    }

    private static string TrimTrailingSlash(string value) => value.Trim().TrimEnd('/');

    private async Task WriteCallLogAsync(
        LlmRequest request,
        string endpoint,
        JsonObject payload,
        DateTimeOffset startedAt,
        long durationMs,
        int? statusCode,
        string? responseText,
        string? errorMessage,
        CancellationToken ct)
    {
        if (!_logRequests)
        {
            return;
        }

        var record = new JsonObject
        {
            ["timestamp_utc"] = startedAt.UtcDateTime.ToString("O"),
            ["provider"] = "OpenAICompatible",
            ["endpoint"] = endpoint,
            ["agent_code"] = request.AgentCode,
            ["model"] = payload["model"]?.GetValue<string>(),
            ["duration_ms"] = durationMs,
            ["status_code"] = statusCode,
            ["request"] = new JsonObject
            {
                ["method"] = "POST",
                ["headers"] = new JsonObject
                {
                    ["authorization"] = "<redacted>",
                    ["content_type"] = "application/json"
                },
                ["body"] = JsonSupport.CloneNode(payload)
            },
            ["response"] = new JsonObject
            {
                ["body"] = ParseLogBody(responseText)
            },
            ["error"] = errorMessage
        };

        Directory.CreateDirectory(_logDirectory);
        var logPath = Path.Combine(_logDirectory, $"llm-calls-{startedAt:yyyyMMdd}.jsonl");
        var line = record.ToJsonString(JsonSupport.SerializerOptions) + Environment.NewLine;

        await LogWriteLock.WaitAsync(ct);
        try
        {
            await File.AppendAllTextAsync(logPath, line, Encoding.UTF8, ct);
        }
        finally
        {
            LogWriteLock.Release();
        }
    }

    private static JsonNode? ParseLogBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            return JsonValue.Create(body);
        }
    }

    private static string SanitizeError(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return string.Empty;
        }

        return responseText.Length <= 1_000 ? responseText : responseText[..1_000];
    }
}
