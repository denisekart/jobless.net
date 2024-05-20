using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using JetBrains.Annotations;

namespace Jobless.UnitTests;

[TestSubject(typeof(SequentialPrioritizedJobDefinitionComparer))]
[ExcludeFromCodeCoverage]
public class SequentialPrioritizedJobDefinitionComparerTests
{
    private readonly IComparer<IJobDefinition> _comparer = SequentialPrioritizedJobDefinitionComparer.Instance;

    [Fact]
    public void Compare_NullValues_ShouldReturnNegativeOne()
    {
        int result = _comparer.Compare(null, null);
        result.Should().Be(-1);
    }

    [Fact]
    public void Compare_SamePriority_DefersToSequenceComparison()
    {
        var firstJob = new TestJobDefinition { Priority = 1, Sequence = 1 };
        var secondJob = new TestJobDefinition { Priority = 1, Sequence = 2 };

        int result = _comparer.Compare(firstJob, secondJob);

        result.Should().BeNegative();
    }

    [Fact]
    public void Compare_DifferentPriority_OrdersBasedOnPriority()
    {
        var firstJob = new TestJobDefinition { Priority = 2, Sequence = 1 };
        var secondJob = new TestJobDefinition { Priority = 1, Sequence = 2 };

        int result = _comparer.Compare(firstJob, secondJob);

        result.Should().BePositive();
    }

    [Fact]
    public void Compare_NoPriority_OrdersBasedOnDefaultPriority()
    {
        var firstJob = new TestJobDefinition { Priority = null, Sequence = 1 };
        var secondJob = new TestJobDefinition { Priority = null, Sequence = 2 };

        int result = _comparer.Compare(firstJob, secondJob);

        result.Should().BeNegative();
    }

    private class TestJobDefinition : IJobDefinition
    {
        public string Key { get; init; } = Guid.NewGuid().ToString();
        public int? Priority { get; set; }
        public long Sequence { get; set; }
    }
}
