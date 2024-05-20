namespace Jobless;

public interface IJobExecutor
{
    Task Execute(IJobDefinition jobDefinition, CancellationToken cancellationToken);
}
