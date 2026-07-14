using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Input;
using OneTouchAutomation.Services.Vision;

namespace Test.Automation;

internal sealed class StubScreenCaptureService : IScreenCaptureService
{
    public CapturedFrame Frame { get; init; } = new()
    {
        PngBytes = [1, 2, 3],
        Width = 800,
        Height = 600,
        CapturedAt = DateTimeOffset.UtcNow
    };

    public int ClientCaptureCount { get; private set; }

    public int ScreenCaptureCount { get; private set; }

    public Task<CapturedFrame> CaptureScreenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ScreenCaptureCount++;
        return Task.FromResult(Frame);
    }

    public Task<CapturedFrame> CaptureWindowAsync(string windowTitleKeyword, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<CaptureWindowInfo>> ListWindowsAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CapturedFrame> CaptureWindowAsync(IntPtr windowHandle, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CapturedFrame> CaptureWindowClientAsync(IntPtr windowHandle, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ClientCaptureCount++;
        return Task.FromResult(Frame);
    }
}

internal sealed class StubVisionDebugService : IVisionDebugService
{
    public required TemplateMatchResult Result { get; init; }

    public List<TemplateMatchRequest> Requests { get; } = new();

    public Task<TemplateMatchResult> MatchTemplateAsync(
        TemplateMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request);
        return Task.FromResult(Result);
    }
}

internal sealed class RecordingInputService : IInputService
{
    public CapturedFrame? ClickFrame { get; private set; }

    public TemplateMatchResult? ClickResult { get; private set; }

    public MouseClickOptions? ClickOptions { get; private set; }

    public List<AutomationKey> PressedKeys { get; } = new();

    public Task MoveMouseAsync(int screenX, int screenY, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task ClickAsync(int screenX, int screenY, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task ClickAsync(
        int screenX,
        int screenY,
        MouseClickOptions options,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PressKeyAsync(AutomationKey key, CancellationToken cancellationToken = default)
    {
        PressedKeys.Add(key);
        return Task.CompletedTask;
    }

    public Task ClickMatchCenterAsync(
        CapturedFrame frame,
        TemplateMatchResult result,
        CancellationToken cancellationToken = default)
    {
        ClickFrame = frame;
        ClickResult = result;
        return Task.CompletedTask;
    }

    public Task ClickMatchAsync(
        CapturedFrame frame,
        TemplateMatchResult result,
        MouseClickOptions options,
        CancellationToken cancellationToken = default)
    {
        ClickFrame = frame;
        ClickResult = result;
        ClickOptions = options;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingWindowActivationService : IWindowActivationService
{
    public List<IntPtr> ActivatedWindows { get; } = new();

    public Task ActivateAsync(IntPtr windowHandle, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ActivatedWindows.Add(windowHandle);
        return Task.CompletedTask;
    }
}
