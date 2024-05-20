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

    internal void Build()
    {
        builder.Services.AddHostedService<BackgroundJobExecutionService>();

        _builder.Services.TryAddSingleton(IJobsExecutionStrategy.Default);
        _builder.Services.TryAddSingleton<IJobManager>();
        _builder.Services.TryAddSingleton(SequentialPrioritizedJobDefinitionComparer.Instance);

        _builder.Services.AddSingleton<JobPriorityComparerFactory>(services =>
            (category) => services.GetKeyedService<IComparer<IJobDefinition>>(category)
                ?? services.GetRequiredService<IComparer<IJobDefinition>>());

        _builder.Services.AddSingleton<JobExecutorFactory>(services => (
            category) => services.GetKeyedService<IJobExecutor>(category) ??
            throw new InvalidOperationException(
                $"Job executor for category {category} was not found. Make sure you register the executor during startup."));
    }
}
