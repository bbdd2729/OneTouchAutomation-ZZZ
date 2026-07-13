namespace OneTouchAutomation.Services.Input;

public sealed class MouseClickOptions
{
    public MouseClickMode Mode { get; init; } = MouseClickMode.Single;

    public int OffsetX { get; init; }

    public int OffsetY { get; init; }

    public int RepeatCount { get; init; } = 1;

    public int IntervalMilliseconds { get; init; } = 100;

    public int HoldDurationMilliseconds { get; init; } = 60;
}
