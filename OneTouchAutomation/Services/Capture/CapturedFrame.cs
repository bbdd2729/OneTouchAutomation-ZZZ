namespace OneTouchAutomation.Services.Capture;

public sealed class CapturedFrame
{
    public required byte[]         PngBytes   { get; init; }
    public required int            Width      { get; init; }
    public required int            Height     { get; init; }
    public required DateTimeOffset CapturedAt { get; init; }
    public          string?        SourceName { get; init; }
    public          int            SourceX    { get; init; }
    public          int            SourceY    { get; init; }
}