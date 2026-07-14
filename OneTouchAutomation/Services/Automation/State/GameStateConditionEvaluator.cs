namespace OneTouchAutomation.Services.Automation.State;

public sealed class GameStateConditionEvaluator : IGameStateConditionEvaluator
{
    private readonly IGameStateStore _stateStore;

    public GameStateConditionEvaluator(IGameStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public bool Evaluate(GameStateConditionGroup conditionGroup)
    {
        ArgumentNullException.ThrowIfNull(conditionGroup);

        if (conditionGroup.Conditions.Count == 0)
        {
            return false;
        }

        var evaluations = conditionGroup.Conditions.Select(EvaluateCondition);

        return conditionGroup.Operator == GameStateConditionOperator.All
            ? evaluations.All(value => value)
            : evaluations.Any(value => value);
    }

    private bool EvaluateCondition(GameStateCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        var isActive = _stateStore.IsActive(
            condition.StateId,
            condition.MinimumConfidence,
            condition.MaximumAge);

        return condition.IsNegated ? !isActive : isActive;
    }
}
