using OneTouchAutomation.Services.Automation.Tasks;
using OneTouchAutomation.Services.Input;

namespace OneTouchAutomation.Services.Automation.Persistence;

public sealed class AutomationTaskConfiguration
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string BehaviorId { get; init; }

    public required string TemplatePath { get; init; }

    public double Threshold { get; init; } = 0.85;

    public bool UseRegion { get; init; }

    public int RegionX { get; init; }

    public int RegionY { get; init; }

    public int RegionWidth { get; init; } = 400;

    public int RegionHeight { get; init; } = 300;

    public int TimeoutSeconds { get; init; } = 10;

    public TaskFailurePolicy FailurePolicy { get; init; } = TaskFailurePolicy.Stop;

    public int MaxRetryCount { get; init; }

    public AutomationKey Key { get; init; } = AutomationKey.Enter;

    public int DelayMilliseconds { get; init; } = 500;

    public int ClickOffsetX { get; init; }

    public int ClickOffsetY { get; init; }

    public MouseClickMode ClickMode { get; init; } = MouseClickMode.Single;

    public int ClickRepeatCount { get; init; } = 1;

    public int ClickIntervalMilliseconds { get; init; } = 100;

    public int ClickHoldDurationMilliseconds { get; init; } = 60;

    public string WorkflowId { get; init; } = string.Empty;

    public int GameRefreshHour { get; init; } = 4;

    public bool ForceDailyRun { get; init; }

    public bool IsEnabled { get; init; } = true;
}
