namespace Jobless;

public record JobState(string State)
{
    public static JobState New => new("new");
    public static JobState Registered => new("registered");
    public static JobState Scheduled => new("scheduled");
    public static JobState Queued => new("queued");
    public static JobState Running => new("running");
    public static JobState Finished => new("finished");
    public static JobState CanceledByUser => new("canceled-by-user");
    public static JobState CanceledBySystem => new("canceled-by-system");
    public static JobState Failed => new("failed");
}