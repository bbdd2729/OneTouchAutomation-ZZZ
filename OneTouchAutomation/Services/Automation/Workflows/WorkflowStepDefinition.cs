using OneTouchAutomation.Services.Automation.Tasks;

namespace OneTouchAutomation.Services.Automation.Workflows;

public sealed class WorkflowStepDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string BehaviorId { get; init; }

    public required object Parameters { get; init; }

    public bool IsEnabled { get; init; } = true;

    public TaskFailurePolicy FailurePolicy { get; init; } = TaskFailurePolicy.Stop;

    public int MaxRetryCount { get; init; }
}
