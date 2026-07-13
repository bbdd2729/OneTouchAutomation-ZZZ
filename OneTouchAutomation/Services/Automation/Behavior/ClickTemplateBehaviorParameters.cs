namespace OneTouchAutomation.Services.Automation.Behavior;

public sealed class ClickTemplateBehaviorParameters
{
    public required string TemplatePath { get; init; }

    public double Threshold { get; init; } = 0.85;

    public bool UseRegion { get; init; }

    public int RegionX { get; init; }

    public int RegionY { get; init; }

    public int RegionWidth { get; init; } = 400;

    public int RegionHeight { get; init; } = 300;

    public int ClickOffsetX { get; init; }

    public int ClickOffsetY { get; init; }

    public OneTouchAutomation.Services.Input.MouseClickMode ClickMode { get; init; } =
        OneTouchAutomation.Services.Input.MouseClickMode.Single;

    public int ClickRepeatCount { get; init; } = 1;

    public int ClickIntervalMilliseconds { get; init; } = 100;

    public int ClickHoldDurationMilliseconds { get; init; } = 60;
}
