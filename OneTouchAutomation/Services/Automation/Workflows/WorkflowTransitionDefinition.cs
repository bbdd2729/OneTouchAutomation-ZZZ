namespace OneTouchAutomation.Services.Automation.Workflows;

public sealed class WorkflowTransitionDefinition
{
    public required string FromNodeId { get; init; }

    public string? ToNodeId { get; init; }

    public WorkflowNodeOutcome Outcome { get; init; }

    public string? ExpectedStatus { get; init; }
}
