namespace OneTouchAutomation.Services.Vision;

public sealed class ScreenElementDefinition
{
    public required string TemplatePath { get; init; }

    public double Threshold { get; init; } = 0.85;

    public bool UseRegion { get; init; }

    public int RegionX { get; init; }

    public int RegionY { get; init; }

    public int RegionWidth { get; init; } = 400;

    public int RegionHeight { get; init; } = 300;
}
