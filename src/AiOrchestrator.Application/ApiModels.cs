using System.Text.Json.Nodes;

namespace AiOrchestrator.Application;

public sealed record ApiResponse<T>(bool Success, T? Data, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static ApiResponse<T> Ok(T data) => new(true, data);
    public static ApiResponse<T> Fail(string errorCode, string errorMessage) => new(false, default, errorCode, errorMessage);
}

public sealed record CreateTaskRequest(string ScenarioCode, string Title, JsonObject Input, string? CreatedBy);

public sealed record ReviewActionRequest(string? Comment, JsonObject? ModifiedContent);
