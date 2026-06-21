namespace OneTouchAutomation.Services.Capture;

public sealed class CapturedFrame
{
    public required byte[]         PngBytes   { get; init; }
    public required int            Width      { get; init; }
    public required int            Height     { get; init; }
    public required DateTimeOffset CapturedAt { get; init; }
}