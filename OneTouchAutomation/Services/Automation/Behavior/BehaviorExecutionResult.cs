namespace OneTouchAutomation.Services.Automation.Behavior;

public sealed class BehaviorExecutionResult
{
    public required bool IsSuccess { get; init; }

    public required string Message { get; init; }

    public double MatchScore { get; init; }

    public int? ScreenX { get; init; }

    public int? ScreenY { get; init; }
}