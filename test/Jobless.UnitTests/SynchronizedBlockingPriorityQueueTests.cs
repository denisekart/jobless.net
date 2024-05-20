using System.Diagnostics;
using FluentAssertions;

namespace Jobless.UnitTests
{
    [Trait("Category", "Unit")]
    public class SynchronizedBlockingPriorityQueueTests
    {
        private SynchronizedBlockingPriorityQueue<IJobDefinition> _queue;
        private IComparer<IJobDefinition> _comparer;

        public SynchronizedBlockingPriorityQueueTests()
        {
            _comparer = new SequentialPrioritizedJobDefinitionComparer();
            _queue = new SynchronizedBlockingPriorityQueue<IJobDefinition>(_comparer);
        }

        [Fact]
        public async Task DequeueAsync_ShouldReturnSameJob()
        {
            var job = new TestJobDefinition();
            _queue.Enqueue(job);
            var returnedJob = await _queue.DequeueAsync(CancellationToken.None);
            returnedJob.Should().BeSameAs(job);
        }

        [Fact]
        public async Task EnqueueDequeue_ShouldReturnExistingJob()
        {
            var job = new TestJobDefinition();
            var newJob = new TestJobDefinition();
            _queue.Enqueue(job);
            var returnedjob = await _queue.DequeueAsync(CancellationToken.None);
            returnedjob.Should().BeSameAs(job);
        }

        [Fact]
        public async Task EnqueueDequeue_WhenEmpty_ShouldReturnSameJob()
        {
            var job = new TestJobDefinition();
            _queue.Enqueue(job);
            var returnedJob = await _queue.DequeueAsync(CancellationToken.None);
            returnedJob.Should().BeSameAs(job);
        }

        [Fact]
        public async Task Dequeue_ShouldKeepSameOrderWithPriority()
        {
            var job1 = new TestJobDefinition { Priority = 1 };
            var job2 = new TestJobDefinition { Priority = 2 };
            var job3 = new TestJobDefinition { Priority = 3 };

            _queue.Enqueue(job1);
            _queue.Enqueue(job2);
            _queue.Enqueue(job3);

            var returnedJob1 = await _queue.DequeueAsync(CancellationToken.None);
            var returnedJob2 = await _queue.DequeueAsync(CancellationToken.None);
            var returnedJob3 = await _queue.DequeueAsync(CancellationToken.None);

            returnedJob1.Priority.Should().Be(1);
            returnedJob2.Priority.Should().Be(2);
            returnedJob3.Priority.Should().Be(3);
        }

        [Fact]
        public async Task Dequeue_ShouldKeepSameOrderWithSequence()
        {
            var job1 = new TestJobDefinition { Sequence = 1 };
            var job2 = new TestJobDefinition { Sequence = 2 };
            var job3 = new TestJobDefinition { Sequence = 3 };

            _queue.Enqueue(job1);
            _queue.Enqueue(job2);
            _queue.Enqueue(job3);

            var returnedJob1 = await _queue.DequeueAsync(CancellationToken.None);
            var returnedJob2 = await _queue.DequeueAsync(CancellationToken.None);
            var returnedJob3 = await _queue.DequeueAsync(CancellationToken.None);

            returnedJob1.Sequence.Should().Be(1);
            returnedJob2.Sequence.Should().Be(2);
            returnedJob3.Sequence.Should().Be(3);
        }

        [Fact]
        public async Task Dequeue_ShouldKeepSameOrderWithSequenceWhenMixed()
        {
            var job1 = new TestJobDefinition { Sequence = 3 };
            var job2 = new TestJobDefinition { Sequence = 1 };
            var job3 = new TestJobDefinition { Sequence = 2 };

            _queue.Enqueue(job1);
            _queue.Enqueue(job2);
            _queue.Enqueue(job3);

            var returnedJob1 = await _queue.DequeueAsync(CancellationToken.None);
            var returnedJob2 = await _queue.DequeueAsync(CancellationToken.None);
            var returnedJob3 = await _queue.DequeueAsync(CancellationToken.None);

            returnedJob1.Sequence.Should().Be(1);
            returnedJob2.Sequence.Should().Be(2);
            returnedJob3.Sequence.Should().Be(3);
        }

        [Fact]
        public async Task Dequeue_ShouldKeepSameOrderWithSequenceWhenAndPrioritize()
        {
            var job1 = new TestJobDefinition { Sequence = 3, Priority = 0 };
            var job2 = new TestJobDefinition { Sequence = 1 };
            var job3 = new TestJobDefinition { Sequence = 2 };

            _queue.Enqueue(job1);
            _queue.Enqueue(job2);
            _queue.Enqueue(job3);

            var returnedJob1 = await _queue.DequeueAsync(CancellationToken.None);
            var returnedJob2 = await _queue.DequeueAsync(CancellationToken.None);
            var returnedJob3 = await _queue.DequeueAsync(CancellationToken.None);

            returnedJob1.Sequence.Should().Be(3);
            returnedJob2.Sequence.Should().Be(1);
            returnedJob3.Sequence.Should().Be(2);
        }

        [Fact]
        public async Task Dequeue_ShouldKeepSameOrderWithMixedPriorityAndSequence()
        {
            var job1 = new TestJobDefinition { Priority = 1, Sequence = 3 };
            var job2 = new TestJobDefinition { Priority = 2, Sequence = 2 };
            var job3 = new TestJobDefinition { Priority = 3, Sequence = 1 };

            _queue.Enqueue(job1);
            _queue.Enqueue(job2);
            _queue.Enqueue(job3);

            var returnedJob1 = await _queue.DequeueAsync(CancellationToken.None);
            var returnedJob2 = await _queue.DequeueAsync(CancellationToken.None);
            var returnedJob3 = await _queue.DequeueAsync(CancellationToken.None);

            returnedJob1.Key.Should().Be(job1.Key);
            returnedJob2.Key.Should().Be(job2.Key);
            returnedJob3.Key.Should().Be(job3.Key);
        }

        [Fact]
        public async Task DequeueAsync_EmptyQueue_ShouldWaitUntilItemIsAdded()
        {
            var job = new TestJobDefinition();
            var stopwatch = new Stopwatch();

            var dequeueTask = Task.Run(async () =>
            {
                stopwatch.Start();
                var returnedJob = await _queue.DequeueAsync(CancellationToken.None);
                stopwatch.Stop();

                returnedJob.Should().BeSameAs(job);
                stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(100);
            });

            // Wait for 100ms before pushing the job to the queue.
            await Task.Delay(100);
            _queue.Enqueue(job);

            await dequeueTask;
        }

        [Fact]
        public async Task DequeueEnqueue_ShouldNotTakeSameJobWhenAnotherExists()
        {
            var job1 = new TestJobDefinition(){Sequence = 2};
            var job2 = new TestJobDefinition(){Sequence = 1};

            _queue.Enqueue(job1);

            var returnedJob1 = await _queue.DequeueEnqueueAsync(job2, CancellationToken.None);

            returnedJob1.Should().BeSameAs(job1);
        }

        private class TestJobDefinition : IJobDefinition
        {
            public string Key { get; init; } = Guid.NewGuid().ToString();
            public int? Priority { get; set; }
            public long Sequence { get; set; }
        }
    }
}
