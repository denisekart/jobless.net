using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Jobless;

public interface IJobManager
{
    public Task<RegisterResult> Register(IJobDescription job);
    public Task<CancelResult> Cancel(IJobDefinition jobDefinition);
    public IJoblessStatistics GetStatistics();
}

internal class DefaultJobManager(Channel<IJobDefinition> channel) : IJobManager
{
    public async Task<RegisterResult> Register(IJobDescription job)
    {
        throw new NotImplementedException();
    }

    public async Task<CancelResult> Cancel(IJobDefinition jobDefinition)
    {
        throw new NotImplementedException();
    }

    public IJoblessStatistics GetStatistics()
    {
        throw new NotImplementedException();
    }
}

internal class JobDemux
{
    
}

public record RegisterResult(bool Success, IJobDefinition? JobDefinition, string? Reason, Exception? Exception)
{
    [MemberNotNull(nameof(JobDefinition))]
    public static RegisterResult FromSuccess(IJobDefinition jobDefinition) => new(true, jobDefinition, null, null);

    public static RegisterResult FromFailure(string? reason = null, Exception? exception = null)
        => new(false, null, reason, exception);
}

public record CancelResult(bool Success, string? Reason, Exception? Exception)
{
    public static CancelResult FromSuccess() => new(true, null, null);

    public static CancelResult FromFailure(string? reason = null, Exception? exception = null)
        => new(false, reason, exception);
}