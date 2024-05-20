namespace Jobless;

public class SynchronizedBlockingPriorityQueue<T>(IComparer<T>? priorityComparer)
{
    private readonly PriorityQueue<T, T> _queue = new(priorityComparer);
    private readonly SemaphoreSlim _queueLimitLock = new(0);
    private readonly object _syncLock = new();

    public void Enqueue(T item)
    {
        lock (_syncLock)
        {
            _queue.Enqueue(item, item);
        }

        _queueLimitLock.Release();
    }

    public async Task<T> DequeueAsync(CancellationToken cancellationToken)
    {
        await _queueLimitLock.WaitAsync(cancellationToken);

        lock (_syncLock)
        {
            return _queue.Dequeue();
        }
    }

    public async Task<T> DequeueEnqueueAsync(T item, CancellationToken cancellationToken)
    {
        lock (_syncLock)
        {
            // returning input item if queue is empty
            // queue items size remains the same
            if (_queue.Count == 0)
            {
                return item;
            }
        }

        await _queueLimitLock.WaitAsync(cancellationToken);

        try
        {
            lock (_syncLock)
            {
                return _queue.DequeueEnqueue(item, item);
            }
        }
        finally
        {
            _queueLimitLock.Release();
        }
    }
}
