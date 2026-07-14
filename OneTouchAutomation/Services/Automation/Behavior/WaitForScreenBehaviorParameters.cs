using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.Services.Automation.Behavior;

public sealed class WaitForScreenBehaviorParameters
{
    public required ScreenDefinition Screen { get; init; }

    public int TimeoutSeconds { get; init; } = 10;

    public int PollIntervalMilliseconds { get; init; } = 250;
}
