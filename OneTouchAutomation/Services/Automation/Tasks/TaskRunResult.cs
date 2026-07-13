using System.Collections.Generic;
using System.Linq;

namespace OneTouchAutomation.Services.Automation.Tasks;

public sealed class TaskRunResult
{
    public required IReadOnlyList<AutomationTaskExecutionResult> TaskResults { get; init; }

    public bool IsCancelled { get; init; }

    public bool HasFailures => TaskResults.Any(result => !result.BehaviorResult.IsSuccess);

    public bool IsSuccess => !IsCancelled && !HasFailures;
}
