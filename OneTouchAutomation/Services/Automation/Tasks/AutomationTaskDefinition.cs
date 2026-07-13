namespace OneTouchAutomation.Services.Automation.Tasks;

public sealed class AutomationTaskDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string BehaviorId { get; init; }

    public required object Parameters { get; init; }

    public bool IsEnabled { get; init; } = true;
}
