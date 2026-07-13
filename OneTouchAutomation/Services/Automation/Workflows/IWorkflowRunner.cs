using System;
using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Automation.Workflows;

public interface IWorkflowRunner
{
    Task<WorkflowRunResult> RunAsync(
        IntPtr windowHandle,
        WorkflowDefinition workflow,
        Action<string>? log = null,
        CancellationToken cancellationToken = default);
}
