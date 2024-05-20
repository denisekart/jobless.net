using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using FluentAssertions;

namespace Jobless.UnitTests
{
    [ExcludeFromCodeCoverage]
    [TestSubject(typeof(JobCancellationTokenFactory))]
    public class JobCancellationTokenFactoryTests
    {
        private JobCancellationTokenFactory _factory;

        public class TestJobDefinition : IJobDefinition
        {
            public string Key { get; init; } = "TestKey";
            public long Sequence { get; set; } = 123;

        }

        public JobCancellationTokenFactoryTests()
        {
            _factory = new JobCancellationTokenFactory();
        }

        [Fact]
        public void ExchangeJobForCancellationToken_ShouldReturnToken()
        {
            var job = new TestJobDefinition();

            var token = _factory.ExchangeJobForCancellationToken(job);

            token.Should().BeOfType<CancellationToken>();
        }

        [Fact]
        public void ExchangeJobForCancellationToken_ShouldReturnSameTokenForSameJob()
        {
            var job = new TestJobDefinition();
            var token1 = _factory.ExchangeJobForCancellationToken(job);
            var token2 = _factory.ExchangeJobForCancellationToken(job);

            token1.Should().Be(token2);
        }

        [Fact]
        public void ExchangeJobForCancellationToken_ShouldReturnDifferentTokenForDifferentJob()
        {
            var job1 = new TestJobDefinition() { Key = "TestKey1", Sequence = 1 };
            var job2 = new TestJobDefinition() { Key = "TestKey2", Sequence = 2 };

            var token1 = _factory.ExchangeJobForCancellationToken(job1);
            var token2 = _factory.ExchangeJobForCancellationToken(job2);

            token1.Should().NotBe(token2);
        }

        [Fact]
        public void TryCancel_ShouldReturnFalseForUntrackedJob()
        {
            var job = new TestJobDefinition();

            var result = _factory.TryCancel(job);

            result.Should().BeFalse();
        }

        [Fact]
        public void TryCancel_ShouldReturnTrueForTrackedJob()
        {
            var job = new TestJobDefinition();
            _factory.ExchangeJobForCancellationToken(job);

            var result = _factory.TryCancel(job);

            result.Should().BeTrue();
        }

        [Fact]
        public void TryCancel_ShouldCancelToken()
        {
            var job = new TestJobDefinition();
            var token = _factory.ExchangeJobForCancellationToken(job);

            _factory.TryCancel(job);

            token.IsCancellationRequested.Should().BeTrue();
        }
    }
}