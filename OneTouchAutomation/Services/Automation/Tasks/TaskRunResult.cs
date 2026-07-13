using System.Collections.Generic;
using System.Linq;

namespace OneTouchAutomation.Services.Automation.Tasks;

public sealed class TaskRunResult
{
    public required IReadOnlyList<AutomationTaskExecutionResult> TaskResults { get; init; }

    public bool IsCancelled { get; init; }

    public bool IsSuccess => !IsCancelled && TaskResults.All(result => result.BehaviorResult.IsSuccess);
}
