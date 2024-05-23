namespace Jobless;

public record TransitionResult(bool Success, string? Reason = null, Exception? Exception = null) : Result(Success, Reason, Exception);