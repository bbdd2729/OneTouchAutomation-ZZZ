using System.Threading;

namespace OneTouchAutomation.Services.Automation.Behavior;

public sealed class BehaviorExecutionContext
{
    public required IntPtr WindowHandle { get; init; }

    public required Action<string> Log { get; init; }

    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;
}