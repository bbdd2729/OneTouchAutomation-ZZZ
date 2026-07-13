using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Automation.Tasks;

public interface ITaskRunner
{
    Task<TaskRunResult> RunAsync(
        IntPtr windowHandle,
        IReadOnlyList<AutomationTaskDefinition> tasks,
        Action<string>? log = null,
        CancellationToken cancellationToken = default);
}
