namespace OneTouchAutomation.Services.Capture;

public sealed class ContinuousCaptureRequest
{
    public ContinuousCaptureSource Source { get; init; } = ContinuousCaptureSource.WindowClient;

    public IntPtr WindowHandle { get; init; }

    public string? WindowTitleKeyword { get; init; }

    public int FramesPerSecond { get; init; } = 20;
}
