using System;
using System.Collections.Generic;
using OneTouchAutomation.Services.Automation.Behavior;

namespace OneTouchAutomation.Services.Automation.Workflows;

public sealed class WorkflowRunResult
{
    public IReadOnlyList<WorkflowStepExecutionResult> StepResults { get; init; } = [];

    public bool IsCancelled { get; init; }

    public WorkflowNodeOutcome FinalOutcome { get; init; } = WorkflowNodeOutcome.Success;

    public bool IsSuccess => !IsCancelled && FinalOutcome == WorkflowNodeOutcome.Success;
}

public sealed class WorkflowStepExecutionResult
{
    public required string StepId { get; init; }

    public required string StepName { get; init; }

    public required string BehaviorId { get; init; }

    public required BehaviorExecutionResult BehaviorResult { get; init; }

    public required TimeSpan Duration { get; init; }

    public int AttemptCount { get; init; }

    public WorkflowNodeOutcome Outcome { get; init; }

    public string Status { get; init; } = string.Empty;
}
