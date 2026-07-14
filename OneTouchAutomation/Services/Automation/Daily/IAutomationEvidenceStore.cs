using System;
using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Automation.Daily;

public interface IAutomationEvidenceStore
{
    Task<string?> SaveWindowCaptureAsync(
        IntPtr windowHandle,
        string workflowId,
        DateOnly gameDay,
        string reason,
        CancellationToken cancellationToken = default);
}
