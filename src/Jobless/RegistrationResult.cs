namespace Jobless;

public record RegistrationResult(bool Success, string? Reason = null, Exception? Exception = null) : Result(Success, Reason, Exception);