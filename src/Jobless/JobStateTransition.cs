namespace Jobless;

public record JobStateTransition(JobState OriginalState, JobState TargetState, string? Reason, Exception? Exception) : JobState(OriginalState)
{
    public static JobStateTransition FromAttemptedTransition(JobState from, JobState to)
        => new JobStateTransition(from, to, $"Transitioning from {from} to {to}", null);
}