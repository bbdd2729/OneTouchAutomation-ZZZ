namespace OneTouchAutomation.Services.Automation.State;

public sealed record AutomationSceneDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string WorkflowId { get; init; }

    public GameStateConditionGroup Conditions { get; init; } = new();

    public int Priority { get; init; }

    public TimeSpan Cooldown { get; init; }

    public bool IsEnabled { get; init; } = true;
}
