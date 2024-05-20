using System.Collections.Concurrent;

namespace Jobless;

internal class JobCancellationTokenFactory
{
    private readonly ConcurrentDictionary<(string key, long sequence, int? priority), CancellationTokenSource> _tokens = new();

    public CancellationToken ExchangeJobForCancellationToken(IJobDefinition job)
        => _tokens.GetOrAdd((job.Key, job.Sequence, job.Priority), _ => new CancellationTokenSource()).Token;

    public bool TryCancel(IJobDefinition job)
    {
        if (!_tokens.TryRemove((job.Key, job.Sequence, job.Priority), out var token))
        {
            return false;
        }

        if (!token.IsCancellationRequested)
        {
            token.Cancel();
        }

        return true;
    }
}
