namespace OneTouchAutomation.Services.Capture;

public sealed class CaptureWindowInfo
{
    public required IntPtr Handle { get; init; }

    public required string Title { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public string DisplayName => $"{Title} ({Width}x{Height})";
}