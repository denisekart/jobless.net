namespace Jobless;

internal class DefaultJobsExecutionStrategy : IJobsExecutionStrategy
{
    public int MaxNumberOfParallelJobs { get; } = Environment.ProcessorCount * 2;
    public int? MaxNumberOfScheduledJobs { get; } = null;
    public int? MaxNumberOfRecurringJobs { get; } = null;
}
