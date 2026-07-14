using OneTouchAutomation.Services.Automation.State;

namespace Test.Automation;

public sealed class GameStateConditionEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluate_AllConditionsRequiresEveryStateToBeActive()
    {
        var store = CreateStore();
        store.Record(CreateState("BattleEnded", 0.95));
        store.Record(CreateState("ConfirmDialog", 0.92));
        var evaluator = new GameStateConditionEvaluator(store);

        var result = evaluator.Evaluate(new GameStateConditionGroup
        {
            Conditions = [CreateCondition("BattleEnded"), CreateCondition("ConfirmDialog")]
        });

        Assert.True(result);
    }

    [Fact]
    public void Evaluate_AnyConditionsAcceptsOneActiveState()
    {
        var store = CreateStore();
        store.Record(CreateState("BattleEnded", 0.95));
        var evaluator = new GameStateConditionEvaluator(store);

        var result = evaluator.Evaluate(new GameStateConditionGroup
        {
            Operator = GameStateConditionOperator.Any,
            Conditions = [CreateCondition("BattleEnded"), CreateCondition("ConfirmDialog")]
        });

        Assert.True(result);
    }

    [Fact]
    public void Evaluate_NegatedConditionPassesWhenStateIsAbsent()
    {
        var evaluator = new GameStateConditionEvaluator(CreateStore());

        var result = evaluator.Evaluate(new GameStateConditionGroup
        {
            Conditions = [CreateCondition("Loading", isNegated: true)]
        });

        Assert.True(result);
    }

    private static InMemoryGameStateStore CreateStore() => new(new FixedTimeProvider(Now));

    private static GameStateRecord CreateState(string id, double confidence) => new()
    {
        StateId = id,
        Confidence = confidence,
        ObservedAt = Now,
    };

    private static GameStateCondition CreateCondition(string id, bool isNegated = false) => new()
    {
        StateId = id,
        IsNegated = isNegated,
        MaximumAge = TimeSpan.FromSeconds(2),
    };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
