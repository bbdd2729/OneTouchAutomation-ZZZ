namespace OneTouchAutomation.Services.Automation.History;

public sealed class TaskRunTaskHistoryEntry
{
    public required string TaskId { get; init; }

    public required string TaskName { get; init; }

    public required string BehaviorId { get; init; }

    public required string Message { get; init; }

    public bool IsSuccess { get; init; }

    public int AttemptCount { get; init; }

    public double MatchScore { get; init; }

    public int? ScreenX { get; init; }

    public int? ScreenY { get; init; }

    public TimeSpan Duration { get; init; }
}
