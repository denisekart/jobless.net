namespace Jobless;

internal class SequentialPrioritizedJobDefinitionComparer : Comparer<IJobDefinition>
{
    private const int DefaultPriority = 1;
    public static readonly IComparer<IJobDefinition> Instance = new SequentialPrioritizedJobDefinitionComparer();

    public override int Compare(IJobDefinition? x, IJobDefinition? y)
    {
        if (x is null || y is null)
        {
            return -1;
        }

        var value = (x.Priority ?? DefaultPriority).CompareTo(y.Priority ?? DefaultPriority);
        if (value == 0)
        {
            value = x.Sequence.CompareTo(y.Sequence);
        }

        return value;
    }
}
