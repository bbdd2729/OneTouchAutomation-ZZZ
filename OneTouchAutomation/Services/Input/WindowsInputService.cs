using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.Services.Input;

public sealed class WindowsInputService : IInputService
{
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp   = 0x0004;

    public Task MoveMouseAsync
    (
        int screenX,
        int screenY,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!SetCursorPos(screenX, screenY))
        {
            throw new InvalidOperationException($"Failed to move mouse: {screenX}, {screenY}");
        }

        return Task.CompletedTask;
    }

    public async Task ClickAsync
    (
        int screenX,
        int screenY,
        CancellationToken cancellationToken = default)
    {
        await MoveMouseAsync(screenX, screenY, cancellationToken);

        mouse_event
            (MouseEventLeftDown,
             0,
             0,
             0,
             UIntPtr.Zero);
        await Task.Delay(60, cancellationToken);
        mouse_event
            (MouseEventLeftUp,
             0,
             0,
             0,
             UIntPtr.Zero);
    }

    public Task ClickMatchCenterAsync
    (
        CapturedFrame frame,
        TemplateMatchResult result,
        CancellationToken cancellationToken = default)
    {
        if (!result.IsMatch)
        {
            throw new InvalidOperationException("No match to click.");
        }

        var bounds = result.MatchBounds;

        var screenX = frame.SourceX + bounds.X + bounds.Width / 2;
        var screenY = frame.SourceY + bounds.Y + bounds.Height / 2;

        return ClickAsync(screenX, screenY, cancellationToken);
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event
    (
        uint dwFlags,
        uint dx,
        uint dy,
        uint dwData,
        UIntPtr dwExtraInfo);
}