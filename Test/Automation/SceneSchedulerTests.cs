using OneTouchAutomation.Services.Automation.State;

namespace Test.Automation;

public sealed class SceneSchedulerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SelectNext_PrefersTheHighestPriorityEligibleScene()
    {
        var scheduler = CreateScheduler("Battle", "ConfirmDialog");

        var scene = scheduler.SelectNext(
        [
            CreateScene("battle", "Battle", priority: 10),
            CreateScene("dialog", "ConfirmDialog", priority: 100)
        ]);

        Assert.NotNull(scene);
        Assert.Equal("dialog", scene.Id);
    }

    [Fact]
    public void SelectNext_SkipsDisabledAndInactiveScenes()
    {
        var scheduler = CreateScheduler("Battle");

        var scene = scheduler.SelectNext(
        [
            CreateScene("disabled", "Battle", priority: 100, isEnabled: false),
            CreateScene("inactive", "Loading", priority: 90),
            CreateScene("eligible", "Battle", priority: 10)
        ]);

        Assert.NotNull(scene);
        Assert.Equal("eligible", scene.Id);
    }

    [Fact]
    public void SelectNext_RespectsTheSceneCooldown()
    {
        var timeProvider = new MutableTimeProvider(Now);
        var scheduler = new SceneScheduler(new ActiveStateEvaluator("Battle"), timeProvider);
        var scene = CreateScene("battle", "Battle", priority: 1, cooldown: TimeSpan.FromSeconds(10));

        scheduler.MarkExecuted(scene);

        Assert.Null(scheduler.SelectNext([scene]));

        timeProvider.Now = Now.AddSeconds(10);

        Assert.Equal(scene, scheduler.SelectNext([scene]));
    }

    private static SceneScheduler CreateScheduler(params string[] activeStates) =>
        new(new ActiveStateEvaluator(activeStates), new MutableTimeProvider(Now));

    private static AutomationSceneDefinition CreateScene(
        string id,
        string stateId,
        int priority,
        TimeSpan? cooldown = null,
        bool isEnabled = true) => new()
    {
        Id = id,
        Name = id,
        WorkflowId = $"{id}-workflow",
        Priority = priority,
        Cooldown = cooldown ?? TimeSpan.Zero,
        IsEnabled = isEnabled,
        Conditions = new GameStateConditionGroup
        {
            Conditions = [new GameStateCondition { StateId = stateId }]
        }
    };

    private sealed class ActiveStateEvaluator(params string[] activeStates) : IGameStateConditionEvaluator
    {
        public bool Evaluate(GameStateConditionGroup conditionGroup) =>
            conditionGroup.Conditions.All(condition => activeStates.Contains(condition.StateId));
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
