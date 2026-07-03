namespace OneTouchAutomation.Services.Vision;

public class TemplateMatchRequest
{
    public required byte[] SourceBytes { get; init; } = Array.Empty<byte>();

    public required byte[] TemplateBytes { get; init; } = Array.Empty<byte>();

    public double Threshold { get; init; } = 0.85;

    public bool UseRegion { get; init; } = false;

    public int RegionX { get; init; }

    public int RegionY { get; init; }

    public int RegionWidth { get; init; }

    public int RegionHeight { get; init; }
}