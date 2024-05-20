namespace Jobless;

internal delegate IComparer<IJobDefinition> JobPriorityComparerFactory(string category);
