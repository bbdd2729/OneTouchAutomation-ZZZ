using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Input;

public sealed class WindowsWindowActivationService : IWindowActivationService
{
    private const int ShowRestore = 9;

    public Task ActivateAsync(IntPtr windowHandle, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if(windowHandle == IntPtr.Zero || !IsWindow(windowHandle))
        {
            throw new InvalidOperationException("Target window is no longer available.");
        }

        if(IsIconic(windowHandle))
        {
            ShowWindow(windowHandle, ShowRestore);
        }

        if(!SetForegroundWindow(windowHandle))
        {
            throw new InvalidOperationException("Failed to activate target window.");
        }

        return Task.CompletedTask;
    }

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
