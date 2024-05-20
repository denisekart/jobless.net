namespace Jobless;

public interface IJobDefinition
{
    public const string DefaultJobCategory = "default";

    string Category => DefaultJobCategory;

    string Key { get; init; }
    int? Priority => null;
    long Sequence { get; }
}
