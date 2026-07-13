using OneTouchAutomation.Services.Automation.Behavior;

namespace OneTouchAutomation.Services.Automation.Tasks;

public sealed class AutomationTaskExecutionResult
{
    public required string TaskId { get; init; }

    public required string TaskName { get; init; }

    public required string BehaviorId { get; init; }

    public required BehaviorExecutionResult BehaviorResult { get; init; }

    public required TimeSpan Duration { get; init; }
}
