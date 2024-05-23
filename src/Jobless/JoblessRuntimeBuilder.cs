using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobless;

public sealed class JoblessRuntimeBuilder(IHostApplicationBuilder builder)
{
    private readonly IHostApplicationBuilder _builder = builder;

    public JoblessRuntimeBuilder WithJobsExecutionStrategy<T>() where T : class, IJobsExecutionStrategy
    {
        _builder.Services.AddSingleton<IJobsExecutionStrategy, T>();

        return this;
    }

    public JoblessRuntimeBuilder WithJobExecutor<T>(string jobCategory = IJobDefinition.DefaultJobCategory) where T : class, IJobExecutor
    {
        _builder.Services.AddKeyedScoped<IJobExecutor, T>(jobCategory);

        return this;
    }

    public JoblessRuntimeBuilder WithJobOrchestrator<T>(string? category = null) where T : class, IJobOrchestrator
    {
        if (category is null)
        {
            AddDecoratedJobOrchestrator<T>();
        }
        else
        {
            AddDecoratedJobOrchestrator<T>(category);
        }

        return this;
    }

    public JoblessRuntimeBuilder WithJobPriorityComparer(IComparer<IJobDefinition> comparer, string? category = null)
    {
        if (category is null)
        {
            _builder.Services.AddSingleton(comparer);
        }
        else
        {
            _builder.Services.AddKeyedSingleton(category, comparer);
        }

        return this;
    }

    private int? _producerChannelLimit = null;

    public JoblessRuntimeBuilder WithBoundedProducerChannel(int limit)
    {
        if (limit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Bounded channel should have a limit of at least 1");
        }

        _producerChannelLimit = limit;
        return this;
    }

    public JoblessRuntimeBuilder WithUnboundedProducerChannel()
    {
        _producerChannelLimit = null;
        return this;
    }

    private void AddDecoratedJobOrchestrator<T>() where T : class, IJobOrchestrator
    {
        _builder.Services.AddScoped<T>();
        _builder.Services.AddScoped<IJobOrchestrator>(s => ActivatorUtilities.CreateInstance<JobOrchestrationAuditor>(s));
    }

    private void TryAddDecoratedJobOrchestrator<T>() where T : class, IJobOrchestrator
    {
        _builder.Services.AddScoped<T>();
        _builder.Services.AddScoped<IJobOrchestrator>(s => ActivatorUtilities.CreateInstance<JobOrchestrationAuditor>(s));
    }

    private void AddDecoratedJobOrchestrator<T>(string key) where T : class, IJobOrchestrator
    {
        _builder.Services.AddKeyedScoped<T>(key);
        _builder.Services.AddKeyedScoped<IJobOrchestrator>(key,
            (s, k) => ActivatorUtilities.CreateInstance<JobOrchestrationAuditor>(s));
    }

    internal void Build()
    {
        _builder.Services.AddHostedService<BackgroundJobExecutionService>();
        _builder.Services.AddSingleton(
            _producerChannelLimit == null
                ? Channel.CreateUnbounded<IJobDefinition>(new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                })
                : Channel.CreateBounded<IJobDefinition>(new BoundedChannelOptions(_producerChannelLimit.Value)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false,
                    FullMode = BoundedChannelFullMode.Wait
                }));

        TryAddDecoratedJobOrchestrator<DefaultInMemoryJobOrchestrator>();
        _builder.Services.TryAddSingleton(IJobsExecutionStrategy.Default);
        _builder.Services.TryAddSingleton<IJobManager>();
        _builder.Services.TryAddSingleton(SequentialPrioritizedJobDefinitionComparer.Instance);

        _builder.Services.AddSingleton<JobOrchestratorFactory>(services =>
            (category) => services.GetKeyedService<IJobOrchestrator>(category)
                ?? services.GetRequiredService<IJobOrchestrator>());

        _builder.Services.AddSingleton<JobPriorityComparerFactory>(services =>
            (category) => services.GetKeyedService<IComparer<IJobDefinition>>(category)
                ?? services.GetRequiredService<IComparer<IJobDefinition>>());

        _builder.Services.AddSingleton<JobExecutorFactory>(services => (
            category) => services.GetKeyedService<IJobExecutor>(category) ??
            throw new InvalidOperationException(
                $"Job executor for category {category} was not found. Make sure you register the executor during startup."));
    }
}

public delegate IJobOrchestrator JobOrchestratorFactory(string category);

public class DefaultInMemoryJobOrchestrator : IJobOrchestrator
{
    public async Task<RegistrationResult> Registering(IJobDefinition jobDefinition, CancellationToken cancellationToken)
    {
        return new RegistrationResult(true);
    }

    public async Task<Result> CanTransition(IJobDefinition message, JobStateTransition stateTransition, CancellationToken cancellationToken)
    {
        return new Result(true);
    }

    public async Task<TransitionResult> Transitioning(IJobDefinition jobDefinition, JobStateTransition stateTransition, CancellationToken cancellationToken)
    {
        return new TransitionResult(true);
    }
}

internal sealed class JobOrchestrationAuditor(IJobOrchestrator service, IJobAuditService[] auditServices, TimeProvider timeProvider) : IJobOrchestrator
{
    private readonly IJobOrchestrator _service = service;
    private readonly IJobAuditService[] _auditServices = auditServices;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<RegistrationResult> Registering(IJobDefinition jobDefinition, CancellationToken cancellationToken)
    {
        var log = new JobAuditEvent(
            jobDefinition,
            JobStateTransition.FromAttemptedTransition(JobState.New, JobState.Registered),
            GetAuditContext());
        
        return await ExecuteWithAuditing(log, cancellationToken, 
            () => _service.Registering(jobDefinition, cancellationToken));
    }

    public Task<Result> CanTransition(IJobDefinition message, JobStateTransition stateTransition, CancellationToken cancellationToken)
    {
        return _service.CanTransition(message, stateTransition, cancellationToken);
    }

    public async Task<TransitionResult> Transitioning(IJobDefinition jobDefinition, JobStateTransition stateTransition, CancellationToken cancellationToken)
    {
        var log = new JobAuditEvent(
            jobDefinition,
            stateTransition,
            GetAuditContext());
        
        return await ExecuteWithAuditing(log, cancellationToken, 
            () => _service.Transitioning(jobDefinition, stateTransition, cancellationToken));

    }

    private Task DoAudit(JobAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        var tasks = _auditServices.Select(s => s.Log(auditEvent, cancellationToken));

        return Task.WhenAll(tasks);
    }

    private JobAuditEvent AsFailed(JobAuditEvent auditEvent, Exception exception)
    {
        return auditEvent with
        {
            Transition = auditEvent.Transition with
            {
                Reason = exception.Message,
                Exception = exception
            },
            Context = auditEvent.Context with
            {
                EventTimeUtc = _timeProvider.GetUtcNow()
            },
            State = JobAuditEvent.OperationState.Completed
        };
    }

    private JobAuditEvent AsCompleted(JobAuditEvent auditEvent)
    {
        return auditEvent with
        {
            Context = auditEvent.Context with
            {
                EventTimeUtc = _timeProvider.GetUtcNow()
            },
            State = JobAuditEvent.OperationState.Completed
        };
    }

    private async Task<T> ExecuteWithAuditing<T>(JobAuditEvent auditEvent, CancellationToken cancellationToken, Func<Task<T>> execute)
    {
        var auditTask = DoAudit(auditEvent, cancellationToken);
        try
        {
            var value = await execute();
            var succeededAudit = DoAudit(AsCompleted(auditEvent), cancellationToken);
            auditTask = Task.WhenAll(auditTask, succeededAudit);
            return value;
        }
        catch (Exception e)
        {
            var failedAudit = DoAudit(AsFailed(auditEvent, e), cancellationToken);
            auditTask = Task.WhenAll(auditTask, failedAudit);
            throw;
        }
        finally
        {
            await auditTask;
        }
    }

    private AuditContext GetAuditContext()
    {
        var ctx = new AuditContext(_timeProvider.GetUtcNow(), Environment.CurrentManagedThreadId);
        return ctx;
    }
}

public interface IJobAuditService
{
    Task Log(JobAuditEvent auditEvent, CancellationToken cancellationToken);
}

public record JobAuditEvent(IJobDefinition Definition, JobStateTransition Transition, AuditContext Context,  JobAuditEvent.OperationState State = JobAuditEvent.OperationState.Started)
{
    public enum OperationState
    {
        Started = 0,
        Completed = 1
    }
}

public record AuditContext(DateTimeOffset EventTimeUtc, int ManagedThreadId);
