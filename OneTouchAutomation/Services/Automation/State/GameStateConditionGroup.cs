namespace OneTouchAutomation.Services.Automation.State;

public sealed record GameStateConditionGroup
{
    public IReadOnlyList<GameStateCondition> Conditions { get; init; } = [];

    public GameStateConditionOperator Operator { get; init; } = GameStateConditionOperator.All;
}

public enum GameStateConditionOperator
{
    All,
    Any,
}
