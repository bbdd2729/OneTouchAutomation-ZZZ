using System.Collections.Generic;

namespace OneTouchAutomation.Services.Automation.Workflows;

public sealed class WorkflowDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<WorkflowStepDefinition> Steps { get; init; } = [];
}
