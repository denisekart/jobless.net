namespace Jobless;

public interface IJobsExecutionStrategy
{
    int MaxNumberOfParallelJobs { get; }
    int? MaxNumberOfScheduledJobs { get; }
    int? MaxNumberOfRecurringJobs { get; }

    static IJobsExecutionStrategy Default => new DefaultJobsExecutionStrategy();
}
