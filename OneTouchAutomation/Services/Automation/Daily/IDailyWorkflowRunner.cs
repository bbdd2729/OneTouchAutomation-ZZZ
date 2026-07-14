using System;
using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Automation.Daily;

public interface IDailyWorkflowRunner
{
    Task<DailyWorkflowRunResult> RunAsync(
        IntPtr windowHandle,
        string workflowId,
        int gameRefreshHour,
        bool forceRun = false,
        Action<string>? log = null,
        CancellationToken cancellationToken = default);
}
