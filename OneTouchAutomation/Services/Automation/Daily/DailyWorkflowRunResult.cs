using OneTouchAutomation.Services.Automation.Workflows;

namespace OneTouchAutomation.Services.Automation.Daily;

public sealed class DailyWorkflowRunResult
{
    public bool WasSkipped { get; init; }

    public required DailyWorkflowRunRecord Record { get; init; }

    public WorkflowRunResult? WorkflowResult { get; init; }
}
