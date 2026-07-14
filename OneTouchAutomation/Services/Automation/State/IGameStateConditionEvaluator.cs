namespace OneTouchAutomation.Services.Automation.State;

public interface IGameStateConditionEvaluator
{
    bool Evaluate(GameStateConditionGroup conditionGroup);
}
