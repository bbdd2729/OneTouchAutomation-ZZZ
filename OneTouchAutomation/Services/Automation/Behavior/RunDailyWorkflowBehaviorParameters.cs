namespace OneTouchAutomation.Services.Automation.Behavior;

public sealed class RunDailyWorkflowBehaviorParameters
{
    public required string WorkflowId { get; init; }

    public int GameRefreshHour { get; init; } = 4;

    public bool ForceRun { get; init; }
}
