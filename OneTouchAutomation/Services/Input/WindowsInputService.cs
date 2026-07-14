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
    private const uint KeyEventKeyUp = 0x0002;

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
        await ClickAsync(screenX, screenY, new MouseClickOptions(), cancellationToken);
    }

    public async Task ClickAsync
    (
        int screenX,
        int screenY,
        MouseClickOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        await MoveMouseAsync(screenX, screenY, cancellationToken);

        var clickCount = GetClickCount(options);

        for(var index = 0; index < clickCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
            await Task.Delay(options.HoldDurationMilliseconds, cancellationToken);
            mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);

            if(index < clickCount - 1)
            {
                await Task.Delay(options.IntervalMilliseconds, cancellationToken);
            }
        }
    }

    public Task ClickMatchCenterAsync
    (
        CapturedFrame frame,
        TemplateMatchResult result,
        CancellationToken cancellationToken = default)
    {
        return ClickMatchAsync(frame, result, new MouseClickOptions(), cancellationToken);
    }

    public Task ClickMatchAsync
    (
        CapturedFrame frame,
        TemplateMatchResult result,
        MouseClickOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if(!result.IsMatch)
        {
            throw new InvalidOperationException("No match to click.");
        }

        var bounds = result.MatchBounds;
        var screenX = frame.SourceX + bounds.X + bounds.Width / 2 + options.OffsetX;
        var screenY = frame.SourceY + bounds.Y + bounds.Height / 2 + options.OffsetY;

        return ClickAsync(screenX, screenY, options, cancellationToken);
    }

    public async Task PressKeyAsync
    (
        AutomationKey key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if(!Enum.IsDefined(key))
        {
            throw new ArgumentOutOfRangeException(nameof(key));
        }

        keybd_event((byte)key, 0, 0, UIntPtr.Zero);
        await Task.Delay(60, cancellationToken);
        keybd_event((byte)key, 0, KeyEventKeyUp, UIntPtr.Zero);
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

    [DllImport("user32.dll")]
    private static extern void keybd_event
    (
        byte bVk,
        byte bScan,
        uint dwFlags,
        UIntPtr dwExtraInfo);

    private static int GetClickCount(MouseClickOptions options)
    {
        if(!Enum.IsDefined(options.Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(options.Mode));
        }

        if(options.HoldDurationMilliseconds < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options.HoldDurationMilliseconds));
        }

        if(options.IntervalMilliseconds < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options.IntervalMilliseconds));
        }

        return options.Mode switch
        {
            MouseClickMode.Single => 1,
            MouseClickMode.Double => 2,
            MouseClickMode.LongPress => 1,
            MouseClickMode.Repeat when options.RepeatCount > 0 => options.RepeatCount,
            MouseClickMode.Repeat => throw new ArgumentOutOfRangeException(nameof(options.RepeatCount)),
            _ => throw new ArgumentOutOfRangeException(nameof(options.Mode))
        };
    }
}
