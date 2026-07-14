namespace OneTouchAutomation.Services.Automation.State;

public sealed record GameStateCondition
{
    public required string StateId { get; init; }

    public double MinimumConfidence { get; init; } = 0.8;

    public TimeSpan MaximumAge { get; init; } = TimeSpan.FromSeconds(2);

    public bool IsNegated { get; init; }
}
