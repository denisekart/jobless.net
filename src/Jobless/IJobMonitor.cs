namespace Jobless;

public interface IJobMonitor
{
    Task JobStateChanged(IJobDefinition jobDefinition, string state);
}
