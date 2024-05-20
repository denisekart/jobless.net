namespace Jobless;

public interface IJobOrchestrator
{
    Task Registering(IJobDefinition jobDefinition, CancellationToken cancellationToken);
    Task<bool> CanStartExecution(IJobDefinition message, CancellationToken cancellationToken);
    Task TransitioningToState(IJobDefinition jobDefinition, JobStateTransition stateTransition, CancellationToken cancellationToken);
}

public record JobState(string State)
{
    public const string New = "new";
    public const string Registered = "registered";
    public const string Scheduled = "scheduled";
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Finished = "finished";
    public const string CanceledByUser = "canceled-by-user";
    public const string CanceledBySystem = "canceled-by-system";
    public const string Failed = "failed";
}


public record JobStateTransition(string OriginalState, string TargetState, string? Reason, Exception? Exception) : JobState(OriginalState)
{
    public static JobStateTransition FromAttemptedTransition(string from, string to) 
        => new JobStateTransition(from, to, $"Transitioning from {from} to {to}", null);

    public static JobStateTransition FromFailedTransition(string from, string to = Failed, string? reason = null, Exception? exception = null)
        => new(from, to, reason, exception);
}
