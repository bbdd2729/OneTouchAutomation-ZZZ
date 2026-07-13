using System.Collections.Generic;

namespace OneTouchAutomation.Services.Automation.Workflows;

public sealed class WorkflowValidationResult
{
    public IReadOnlyList<string> Errors { get; init; } = [];

    public bool IsValid => Errors.Count == 0;
}
