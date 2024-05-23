using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jobless;

internal class BackgroundJobExecutionService(
    Channel<IJobDefinition> channel,
    JobCancellationTokenFactory cancellationTokenFactory,
    IServiceScopeFactory serviceScopeFactory,
    IJobsExecutionStrategy jobsExecutionStrategy,
    IJobMonitor jobMonitor,
    IComparer<IJobDefinition> jobPriorityComparer) : BackgroundService
{
    private readonly ConcurrentDictionary<Task, IJobDefinition> _workerPool = new();

    private readonly SemaphoreSlim _executorsLock = new(jobsExecutionStrategy.MaxNumberOfParallelJobs);

    private Task? _jobCompletionMonitor;

    private Task? _jobQueueMonitor;

    private readonly SynchronizedBlockingPriorityQueue<IJobDefinition> _jobQueue = new(jobPriorityComparer);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _jobCompletionMonitor = Task.Factory.StartNew(() => JobCompletionMonitorTask(stoppingToken), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        _jobQueueMonitor = Task.Factory.StartNew(() => JobQueueMonitorTask(stoppingToken), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var jobDefinition = await channel.Reader.ReadAsync(stoppingToken);
                _jobQueue.Enqueue(jobDefinition);
            }
        }
        catch (OperationCanceledException)
        {
            // do nothing - we will always break out of the loop when cancellation was required, so we'll handle everything inside TearDown();
        }

        await TearDown();
    }

    private async Task JobCompletionMonitorTask(CancellationToken stoppingToken)
    {
        const int emptyPoolLoopDelayMs = 100;
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_workerPool.IsEmpty)
            {
                // pool is empty - wait for a while and retry
                await Task.Delay(emptyPoolLoopDelayMs, stoppingToken);
                continue;
            }

            var completedTask = await Task.WhenAny(_workerPool.Keys);
            if (!_workerPool.TryRemove(completedTask, out var jobDefinition))
            {
                // someone else already removed the item, cycle around (this shouldn't happen)
                continue;
            }

            _executorsLock.Release();

            await HandleTaskCompletion(completedTask, jobDefinition);
        }
    }

    private async Task HandleTaskCompletion(Task task, IJobDefinition message)
    {
        var tokenForTask = cancellationTokenFactory.ExchangeJobForCancellationToken(message);
        try
        {
            await task;
        }
        catch (OperationCanceledException oce) when (tokenForTask.IsCancellationRequested)
        {
            // job was canceled by user interaction
        }
        catch (OperationCanceledException oce)
        {
            // job was canceled by the application (interrupted)
        }
        catch (Exception e)
        {
            // job did not complete successfully
        }
    }

    private async Task StartNewJob(IJobDefinition message, CancellationToken stoppingToken)
    {
        await _executorsLock.WaitAsync(stoppingToken);

        var jobCancellationToken = cancellationTokenFactory.ExchangeJobForCancellationToken(message);
        var fullCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, jobCancellationToken);
        var newJob = Task.Run(() => JobTask(message, fullCancellationTokenSource.Token), fullCancellationTokenSource.Token);

        if (!_workerPool.TryAdd(newJob, message))
        {
            // log: the task that was started already exists. It was explicitly canceled - this should never happen
            await fullCancellationTokenSource.CancelAsync();
            await HandleTaskCompletion(newJob, message);
        }
    }

    private async Task JobQueueMonitorTask(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await GetNextRunnableJob(cancellationToken);
                await StartNewJob(message, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // do nothing - TearDown will handle the cancellation
        }
    }

    private async Task<IJobDefinition> GetNextRunnableJob(CancellationToken cancellationToken)
    {
        const int loopWaitDelayMs = 100;

        var message = await _jobQueue.DequeueAsync(cancellationToken);
        using var scope = serviceScopeFactory.CreateScope();
        var defaultOrchestrator = scope.ServiceProvider.GetRequiredService<IJobOrchestrator>();
        var orchestrator = scope.ServiceProvider.GetKeyedService<IJobOrchestrator>(message.Category);

        var attemptedTransition = JobStateTransition.FromAttemptedTransition(JobState.Queued, JobState.Running);
        while (await (orchestrator ?? defaultOrchestrator).CanTransition(message, attemptedTransition, cancellationToken)
               is not { Success: true })
        {
            var newMessage = await _jobQueue.DequeueEnqueueAsync(message, cancellationToken);
            if (ReferenceEquals(message, newMessage))
            {
                // we received the same item back - wait for a while and retry
                await Task.Delay(loopWaitDelayMs, cancellationToken);
                message = newMessage;
            }
            else
            {
                message = newMessage;
                orchestrator = scope.ServiceProvider.GetKeyedService<IJobOrchestrator>(message.Category);
            }
        }

        return message;
    }

    private async Task TearDown()
    {
        channel.Writer.Complete();

        if (_jobCompletionMonitor?.IsCompletedSuccessfully is true
            && _jobQueueMonitor?.IsCompletedSuccessfully is true)
        {
            // the monitor has done its job
            return;
        }

        if (_workerPool.Where(x => !x.Key.IsCompleted)
                .ToList()
            is { Count: > 0 } unfinishedTasks)
        {
            foreach (var task in unfinishedTasks)
            {
                await HandleTaskCompletion(task.Key, task.Value);
            }
        }
    }

    private async Task JobTask(IJobDefinition jobDefinition, CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<JobExecutorFactory>()(jobDefinition.Category);
        await executor.Execute(jobDefinition, stoppingToken);
    }
}
