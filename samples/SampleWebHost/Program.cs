using Jobless;

var builder = WebApplication.CreateBuilder(args);

builder.AddJobless(configure => configure
    .WithJobExecutor<SampleJobExecutor>());

var app = builder.Build();

app.UseJobless();

app.Run();

class SampleJobExecutor : IJobExecutor
{
    public Task Execute(IJobDefinition jobDefinition, CancellationToken cancellationToken)
    {
        return Task.Delay(Random.Shared.Next(50, 200), cancellationToken);
    }
}
