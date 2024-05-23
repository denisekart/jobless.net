namespace Jobless;

public record Result(bool Success, string? Reason = null, Exception? Exception = null);